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
    class Tester
    {
        public string t { get; set; }
    }

    public class VdfParser
    {
        // If we dont use a using, how does this affect memory?
        private StreamReader vdfFileReader;

        /// <summary>
        /// Not yet implemented
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vdfFile"></param>
        /// <returns></returns>
        public T Parse<T>(Stream vdfFile)
        {
            dynamic result = Parse(vdfFile);

            T mappedResult = (T)Map(typeof(T), result);

            return mappedResult;
        }

        /// <summary>
        /// Parses a Stream and returns an Expando object representing the VDF file
        /// </summary>
        /// <param name="vdfFile"></param>
        /// <returns></returns>
        public dynamic Parse(Stream vdfFile)
        {
            dynamic result = new ExpandoObject() as IDictionary<string, dynamic>;

            using (vdfFileReader = new StreamReader(vdfFile))
            {
                result = ParseObject();
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private object Map(Type objectType, ExpandoObject source, bool isDictionaryType = false)
        {
            //We could have used a generic method, but because the type will be reflected when it is called recursively, this is the easier solution

            var bindgings = new List<MemberBinding>();
            var src = source as IDictionary<string, object>;
            object returnObj;

            if (isDictionaryType)
            {
                Type[] dictionaryTypes = objectType.GetGenericArguments();

                Type genericDictionaryType = typeof(Dictionary<,>);
                Type constructedDictionaryType = genericDictionaryType.MakeGenericType(dictionaryTypes);

                var constructedObject = Activator.CreateInstance(constructedDictionaryType);

                var returnDictionary = constructedObject as Dictionary<string, dynamic>;

                var keys = src.Keys;

                foreach (var key in keys)
                {
                    var value = (source as IDictionary<string, dynamic>)[key];

                    if (value.GetType() == typeof(string))
                    {
                        returnDictionary.Add(key, value);
                    }

                    returnDictionary.Add(key, Map(dictionaryTypes[1], value, false));
                }

                returnObj = (object)returnDictionary;
            }
            else
            {
                returnObj = Activator.CreateInstance(objectType);
                foreach (PropertyInfo destinationProperty in objectType.GetProperties().Where(x => x.CanWrite))
                {
                    SetProperty(destinationProperty, src, returnObj);
                }
            }

            return returnObj;
        }

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
                    bool isDictionary = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
                    destinationProperty.SetValue(objectInstance, Map(propertyType, val as ExpandoObject, isDictionary));
                }
                else
                {
                    destinationProperty.SetValue(objectInstance, (string)val);
                }
            }
        }


        /// <summary>
        /// The main method that reads all the keys and values in the object
        /// </summary>
        /// <returns></returns>
        private ExpandoObject ParseObject()
        {
            var result = new ExpandoObject() as IDictionary<string, dynamic>;

            while ( ReadKeyValue(out KeyValuePair<string, dynamic> kvp))
            { 
                result[kvp.Key] = kvp.Value;
            }

            return result as ExpandoObject;
        }
        
        /// <summary>
        /// Reads a key and value pair. Calls ParseObject recursively if the value is an object
        /// </summary>
        /// <param name="kvp"></param>
        /// <returns></returns>
        private bool ReadKeyValue(out KeyValuePair<string, dynamic> kvp)
        {
            // If we found a closing brace instead of a key
            if(!ReadNextKey(out string key))
            {
                kvp = new KeyValuePair<string, dynamic>(); // just create an empty object. Won't be used anyway.
                return false;
            }

            while (!vdfFileReader.EndOfStream)
            {
                char c = (char)vdfFileReader.Read();

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
            while (!vdfFileReader.EndOfStream)
            {
                char c = (char)vdfFileReader.Read();

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

            // End of file, but we probably shouldn't reach this.
            Console.WriteLine("Unexpected end of file?");
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
