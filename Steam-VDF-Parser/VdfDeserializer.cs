using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using VdfParser.Enums;


namespace VdfParser
{
    public class VdfDeserializer
    {
        private enum CollectionTypes
        {
            None,
            List,
            Dictionary
        }

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

        // <summary>
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
        /// <param name="isDictionaryType">Is the targetObject a real dictionary?</param>
        /// <returns></returns>
        //We could have used a generic method, but because the type will be reflected when it is called recursively, this is the easier solution
        private object Map(Type targetObjectType, ExpandoObject source, CollectionTypes colType = CollectionTypes.None)
        {
            var src = source as IDictionary<string, dynamic>;
            object returnObj;

            // if it is a dictionary, we need to jump throuh some reflection hoops to instantiate the correct types
            if (colType == CollectionTypes.Dictionary)
            {
                // Instantiate the correct dictionary type
                Type[] dictionaryTypes = targetObjectType.GetGenericArguments();
                Type genericDictionaryType = typeof(Dictionary<,>);
                Type constructedDictionaryType = genericDictionaryType.MakeGenericType(dictionaryTypes);
                var returnDictionary = Activator.CreateInstance(constructedDictionaryType);

                // Loop through the values in the source, and add them to the dictionary
                var keys = src.Keys;
                foreach (var key in keys)
                {
                    var value = src[key];

                    // If the value type is string, then we don't need to instantiate the value and go through the reflection code again.
                    if (value.GetType() == typeof(string))
                    {
                        var arguments = new object[] { key, value };
                        constructedDictionaryType.InvokeMember("Add", BindingFlags.InvokeMethod, null, returnDictionary, arguments);
                    }
                    else // It is a complex object, so we need to instantiate and map to a new object before adding it to the dictionary
                    {
                        var arguments = new object[]
                        {
                            key,
                            Map(dictionaryTypes[1], value, CollectionTypes.None) // dictionaryTypes[1] is the specified type it should be cast to.
                        };

                        constructedDictionaryType.InvokeMember("Add", BindingFlags.InvokeMethod, null, returnDictionary, arguments);
                    }
                }

                returnObj = (object)returnDictionary;
            }
            else if (colType == CollectionTypes.List)
            {
                Type[] listTypes = targetObjectType.GetGenericArguments();
                Type genericListType = typeof(List<>);
                Type constructedListType = genericListType.MakeGenericType(listTypes);
                var returnList = Activator.CreateInstance(constructedListType);

                var keys = src.Keys; // Should only be indexes

                foreach(var key in keys)
                {
                    var value = src[key];

                    // If the value type is string, then we don't need to instantiate the value and go through the reflection code again.
                    if (value.GetType() == typeof(string))
                    {
                        var arguments = new object[] { value };
                        constructedListType.InvokeMember("Add", BindingFlags.InvokeMethod, null, returnList, arguments);
                    }
                    else // It is a complex object, so we need to instantiate and map to a new object before adding it to the List
                    {
                        var arguments = new object[]
                        {
                            Map(listTypes[0], value, CollectionTypes.None) // dictionaryTypes[1] is the specified type it should be cast to.
                        };

                        constructedListType.InvokeMember("Add", BindingFlags.InvokeMethod, null, returnList, arguments);
                    }
                }

                returnObj = (object)returnList;
            }
            else // It is not a dictionary, so it is a simple property on an object that should be assigned a value.
            {
                returnObj = Activator.CreateInstance(targetObjectType);
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
                    CollectionTypes colType;

                    if (propertyType.GetInterfaces()
                        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                    {
                        colType = CollectionTypes.Dictionary;
                    }
                    else if (propertyType.GetInterfaces()
                        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>)))
                    {
                        colType = CollectionTypes.List;
                    }
                    else
                    {
                        colType = CollectionTypes.None;
                    }
                    
                    destinationProperty.SetValue(objectInstance, Map(propertyType, val as ExpandoObject, colType));
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
                switch (c)
                {
                    // Whitespace can be ignored
                    case (char)WhitespaceCharacters.CarriageReturn:
                    case (char)WhitespaceCharacters.NewLine:
                    case (char)WhitespaceCharacters.Space:
                    case (char)WhitespaceCharacters.Tab:
                        continue;

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

            throw new Exception("Busy reading object, but reached end of file without a closing breace");
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
                switch (c)
                {
                    // Whitespace can be ignored
                    case (char)WhitespaceCharacters.CarriageReturn:
                    case (char)WhitespaceCharacters.NewLine:
                    case (char)WhitespaceCharacters.Space:
                    case (char)WhitespaceCharacters.Tab:
                        Console.Write(c.ToString());
                        continue;
                        
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
            StringBuilder sb = new StringBuilder();

            while (cont)
            {
                char c = (char)vdfFileReader.Read();
                Console.WriteLine(c.ToString());

                //TODO - Check for escape characters: \n, \t, \\, and \"
                // But do we really have to? We can just ignore it, cant we?

                // Check for space or tab if there wasn't a starting quote, or a double quote
                // What about new line?
                if ((startedWithQuote && c.Equals((char)ControlCharacters.Quote)) ||
                    (!startedWithQuote &&
                        (c.Equals((char)WhitespaceCharacters.CarriageReturn) ||
                        c.Equals((char)WhitespaceCharacters.NewLine) ||
                        c.Equals((char)WhitespaceCharacters.Space) ||
                        c.Equals((char)WhitespaceCharacters.Tab)
                        )
                    ))
                {
                    cont = false;
                }
                else
                {
                    sb.Append(c.ToString());
                }
            }

            return sb.ToString();
        }
    }
}
