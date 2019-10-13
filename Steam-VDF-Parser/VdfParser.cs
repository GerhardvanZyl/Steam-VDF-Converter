using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using VdfParser.Enums;


namespace VdfParser
{
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
            throw new NotImplementedException();
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
