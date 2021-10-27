using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VdfConverter;
using VdfConverter.Enums;

namespace VdfParser
{
    public class VdfDeserializer : VdfBase
    {
        // If we dont use a using, how does this affect memory?
        private TextReader vdfFileReader;

        /// <summary>
        /// Parse the VDF Stream and casts it to the supplied type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="vdfFile">The Stream representing the VDF file</param>
        /// <returns></returns>
        public T Deserialize<T>(Stream vdfFile)
        {
            dynamic result = Deserialize(vdfFile);

            T mappedResult = (T)Map(typeof(T), result);

            return mappedResult;
        }

        /// <summary>
        /// Parse the VDF Stream and casts it to the supplied type
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="vdfFile">The string representing the VDF file</param>
        /// <returns></returns>
        public T Deserialize<T>(string vdfFile)
        {
            dynamic result = Deserialize(vdfFile);

            T mappedResult = (T)Map(typeof(T), result);

            return mappedResult;
        }

        /// <summary>
        /// Parses a Stream and returns an Expando object representing the VDF file.
        /// </summary>
        /// <param name="vdfFile">The Stream representing the VDF file</param>
        /// <returns></returns>
        public dynamic Deserialize(Stream vdfFile)
        {
            dynamic result = new ExpandoObject() as IDictionary<string, dynamic>;

            // Using the vdfFileReader to the member variable.
            using (vdfFileReader = new StreamReader(vdfFile))
            {
                result = ParseObject();
            }

            return result;
        }

        /// <summary>
        /// Parses a string and returns an Expando object representing the VDF file.
        /// </summary>
        /// <param name="vdfFile">The string representing the VDF file</param>
        /// <returns></returns>
        public dynamic Deserialize(string vdfFile)
        {
            dynamic result = new ExpandoObject() as IDictionary<string, dynamic>;

            // Using the vdfFileReader to the member variable.
            using (vdfFileReader = new StringReader(vdfFile))
            {
                result = ParseObject();
            }

            return result;
        }

        /// <summary>
        /// Maps the properties of the values read from the VDF file, to the object created from the specified type
        /// </summary>
        /// <param name="targetObjectType">Type of the object to map to</param>
        /// <param name="source">Source from which to read the value</param>
        /// <returns></returns>

        // We could have used a generic method, but because the type will be reflected when it is called recursively, this is the easier solution
        private object Map(Type targetObjectType, ExpandoObject source)
        {
            // Expando Object is just a fancy dictionary.
            var src = source as IDictionary<string, dynamic>;
            
            // TODO: Check if it is generic first
            Type[] genericArguments = targetObjectType.GetGenericArguments();
            dynamic returnObj;

            try
            {
                returnObj = (dynamic)Activator.CreateInstance(targetObjectType);
            }
            catch (Exception ex)
            {
                try
                {
                    var keys = src.Keys;
                    StringBuilder sb = new StringBuilder();

                    foreach(var key in keys)
                    {
                        sb.Append($"Key: {key} - ");
                        var val = src[key].ToString();
                        sb.Append($"Value: {val} |");
                    }

                    throw new VdfTypeException($"Error trying to instantiate object of type {targetObjectType.Name}, while trying to cast {sb.ToString()} ", ex);
                }
                catch (Exception) // just make sure that we don't throw an exception while createing the exception message.
                {
                    throw new VdfTypeException($"Error trying to instantiate object of type {targetObjectType.Name}", ex);
                }
            }

            // We need a collection for the Add method
            bool isCollection = IsCollection(targetObjectType);
            if (isCollection)
            {
                // Loop through the values in the source, and add them to the IEnumerable (probably a dictionary)
                var keys = src.Keys;
                foreach (var key in keys)
                {
                    var value = src[key];

                    // If the value type is string, then we don't need to instantiate the value and go through the reflection code again,
                    // otherwise we need to instantiate and map.
                    // We can just check for string, since it is the only simple type we read from the file/string
                    if (value.GetType() != typeof(string)) 
                    {
                        value = Map(genericArguments[1], value);
                    }

                    // We assume that Add either takes one or two parameters
                    // And at the moment, we will use the lazy try catch method
                    try
                    {
                        returnObj.Add(key, value);
                    }
                    catch (Exception e1) // TODO: use the correct exception for .Net Standard
                    {
                        if(e1.Message.Contains("invalid arguments"))
                        {
                            throw new VdfFormatException($"There is an error in the format of the VDF file/string for Property: {key} and Value: {value}");
                        }

                        try
                        {
                            returnObj.Add(value);
                        }
                        catch(Exception e2) // TODO: use the correct exception for .Net Standard
                        {
                            throw new VdfTypeException($"Error adding value to ICollection. Key: {key}. Value: {src[key]}", e2);
                        }
                    }
                }
            }
            else // It is not a dictionary, so it is a simple property on an object that should be assigned a value.
            {
                foreach (PropertyInfo destinationProperty in targetObjectType.GetProperties().Where(x => x.CanWrite))
                {
                    SetProperty(destinationProperty, src, returnObj);
                }
            }

            return returnObj;
        }

        /// <summary>
        /// Sets the value of a property on an object based on a key value pair in a dictionary
        /// </summary>
        /// <param name="destinationProperty"></param>
        /// <param name="sourceValue"></param>
        /// <param name="objectInstance"></param>
        private void SetProperty(PropertyInfo destinationProperty, IDictionary<string, object> sourceValue, object objectInstance)
        {
            string key = sourceValue.Keys.SingleOrDefault(x => x.Equals(destinationProperty.Name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(key))
            {
                var val = sourceValue[key];
                Type propertyType = destinationProperty.PropertyType;

                // Cant do switch, typeof(type) is not a constant type
                // So far we have bool, datetime, string and another object
                // Any other types we should cater for?
                if (propertyType == typeof(bool))
                {
                    // if the type is boolean, then this source type would have been read as a string (or should have been)
                    // Bool is used as 1 or 0.
                    // TODO - cater for other types later
                    destinationProperty.SetValue(objectInstance, (string)val == "1");
                }
                else if (propertyType == typeof(DateTime))
                {
                    // if the type is boolean, then this source type would have been read as a string (or should have been)
                    destinationProperty.SetValue(objectInstance, DateTime.Parse((string)val));
                }
                else if (val.GetType() == typeof(ExpandoObject))
                {
                    destinationProperty.SetValue(objectInstance, Map(propertyType, val as ExpandoObject));
                }
                else
                {
                    destinationProperty.SetValue(objectInstance, (string)val);
                }
            }
        }


        /// <summary>
        /// The main method that reads all the keys and values in the object, and assigns it to an expando object
        /// </summary>
        /// <returns>ExpandoObject</returns>
        private ExpandoObject ParseObject()
        {
            var result = new ExpandoObject() as IDictionary<string, dynamic>;

            // Reading the key and value pairs from the file using the ReadKeyValue method
            while ( ReadKeyValue(out KeyValuePair<string, dynamic> kvp))
            { 
                result[kvp.Key] = kvp.Value;
            }

            // This will be the complete object that is returned
            return result as ExpandoObject;
        }
        
        /// <summary>
        /// Reads a key and value pair. Calls ParseObject recursively if the value is an object
        /// </summary>
        /// <param name="kvp">Key Value Pair read in the VDF file</param>
        /// <returns>A boolean, indication whether reading should continue</returns>
        private bool ReadKeyValue(out KeyValuePair<string, dynamic> kvp)
        {
            // If we find the end of the object, we need to return false.
            if(!ReadNextKey(out string key))
            {
                // just create an empty dictionary to satisfy return signature requirements. Won't be used anyway.
                kvp = new KeyValuePair<string, dynamic>(); 
                return false;
            }


            int tmp;
            while((tmp = vdfFileReader.Read()) != -1)
            {
                char c = (char)tmp;

                // We can either call the method, or list all the characters individually in the switch. So...
                if (IsWhiteSpace(c)) continue;

                switch (c)
                {
                    // Strart of an object
                    case (char)ControlCharacters.OpenBrace:
                        // read new object
                        kvp = new KeyValuePair<string, dynamic>(key, ParseObject());
                        return true;

                    // Closing brace means we're done with this object
                    case (char)ControlCharacters.CloseBrace:
                        kvp = new KeyValuePair<string, dynamic>(); // just create an empty object. Won't be used anyway.
                        return false; 
                    
                    // In this case it will be the beginning. We will read the closing quote in the ReadString method
                    case (char)ControlCharacters.Quote:
                        kvp = new KeyValuePair<string, dynamic>(key, ReadString(true));
                        return true;

                    // It's probably a string value without an opening quote. Should have errored by now if it wasnt'
                    default:
                        // Add read character, as it will be the first part of the string due to not starting with a quote
                        kvp = new KeyValuePair<string, dynamic>(key, c.ToString() + ReadString(false));
                        return true;
                }
            }

            throw new VdfFormatException("Busy reading object, but reached end of file without a closing breace");
        }

        /// <summary>
        /// Reads the next key in the file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool ReadNextKey(out string key)
        {
            int tmp;
            while ((tmp = vdfFileReader.Read()) != -1)
            {
                char c = (char)tmp;

                // We can either call the method, or list all the characters individually in the switch. So...
                if (IsWhiteSpace(c)) continue;

                switch (c)
                {
                    // Strart of an object
                    case (char)ControlCharacters.OpenBrace:
                    case (char)ControlCharacters.CloseBrace:
                        key = "";
                        return false;

                    // In this case it will be the beginning. We will read the closing quote in the ReadString method
                    case (char)ControlCharacters.Quote:
                        key = ReadString(true);
                        return true;

                    // It's probably a string value without an opening quote. Should have errored by now if it wasnt'
                    default:
                        // Add the read string, since it's the first letter of the string
                        key = c.ToString() + ReadString(false);
                        return true;
                }
            }

            key = "";
            return false;
        }

        /// <summary>
        /// Reads a string value until it finds the next control character or whitespace, 
        /// depending on whether the string value is enclosed in quotes
        /// </summary>
        /// <param name="startedWithQuote"></param>
        /// <returns></returns>
        private string ReadString(bool startedWithQuote = false)
        {
            bool cont = true;
            bool shouldSkipNextCharacter = false;
            StringBuilder sb = new StringBuilder();

            while (cont)
            {
                char c = (char)vdfFileReader.Read();

                if (shouldSkipNextCharacter)
                {
                    shouldSkipNextCharacter = false;
                    continue;
                }

                //TODO - Check for escape characters: \n, \t, \\, and \"
                // But do we really have to? We can just ignore it, cant we?

                // Check for space or tab if there wasn't a starting quote, or a double quote
                // What about new line?
                if ((startedWithQuote && c.Equals((char)ControlCharacters.Quote)) 
                    || (!startedWithQuote && IsWhiteSpace(c)))
                {
                    cont = false;
                }
                else if (c.Equals((char)ControlCharacters.BackSlash) 
                    && vdfFileReader.Peek().Equals((char)ControlCharacters.Quote))
                {
                    // If there is an escaped quote, add it and continue parsing the string.
                    shouldSkipNextCharacter = true;
                    sb.Append("\"");
                }
                else
                {
                    sb.Append(c.ToString());
                }
            }

            return sb.ToString();
        }

        private bool IsWhiteSpace(char c)
        {
            // Don't do type checking for now. It should fail if the character isn't an int.
            return Enum.IsDefined(typeof(WhitespaceCharacters), (int)c);
        }
    }
}
