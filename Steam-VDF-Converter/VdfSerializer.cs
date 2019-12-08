using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VdfConverter.Enums;

namespace VdfConverter
{
    public class VdfSerializer : VdfBase
    {
        private TextWriter _writer;
        private int _indentSize;

        /// <summary>
        /// Serialze the object and outputs a string in the VDF format
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Serialize(object content)
        {
            using (_writer = new StringWriter())
            {
                WriteProperties(content);
            }

            return _writer.ToString();
        }

        /// <summary>
        /// Serializes each property in the object to the writer
        /// </summary>
        /// <param name="obj"></param>
        private void WriteProperties(object obj)
        {
            Type objType = obj.GetType();

            objType.GetProperties()
                .Where(x => x.CanWrite)
                .ToList()
                .ForEach( x => {
                    WriteProperty(x.Name);
                    WriteValue(x.GetValue(obj));
                });
        }

        /// <summary>
        /// Writes a property (key) to the writer.
        /// </summary>
        /// <param name="property"></param>
        private void WriteProperty(string property)
        {
            InsertTabs();
            WriteString(property);
            _writer.Write((char)WhitespaceCharacters.Tab);
        }

        /// <summary>
        /// Writes a string value surrounded by double quotes
        /// </summary>
        /// <param name="val"></param>
        private void WriteString(string val)
        {
            _writer.Write("\"");
            _writer.Write(val);
            _writer.Write("\"");
        }

        /// <summary>
        /// Write a "value"s to the writer.
        /// </summary>
        /// <param name="value"></param>
        private void WriteValue(object value)
        {
            _writer.Write((char)WhitespaceCharacters.Tab);
            
            Type valueType = value.GetType();

            if (valueType == typeof(string))
            {
                WriteString(value.ToString());
                InsertNewLine();
            }
            else if (valueType == typeof(bool))
            {
                // the value is of type boolean, but we can't just cast to bool, so cast to string.
                // We could then tryparse, but since we will anyway write 1 or 0, we can just to a equality comparison.
                WriteString(value.ToString().ToLowerInvariant() == "true" ? "1" : "0"); // Hacky way to try parse and default to false (0)
                InsertNewLine();
            }
            else if (valueType == typeof(DateTime))
            {
                WriteString(((DateTime)value).ToString("yyyy-dd-MM"));
                InsertNewLine();
            }
            else if (valueType.IsGenericType && IsCollection(value.GetType()))
            {
                StartObject();
                WriteCollection(value);
                EndObject();
            }
            else // another object
            {
                StartObject();
                WriteProperties(value);
                EndObject();
            }
        }

        /// <summary>
        /// Write a Collection of values (like Dictionary or List) to a file
        /// </summary>
        /// <param name="value"></param>
        private void WriteCollection(object value)
        {
            Type objectType = value.GetType();
            Type[] genericArguments = objectType.GetGenericArguments();
            dynamic dynamicKeyValues = (dynamic)value;

            // Is an index based collection
            if (genericArguments.Length == 1)
            {
                // Collections can't necessarily be accessed form the index, and needs to be enumerated.
                // Therefore we need to handle the index ourselves
                int i = 0;
                foreach (var obj in dynamicKeyValues)
                {
                    WriteProperty(i.ToString());
                    InsertTabs();
                    WriteValue(obj);
                    i++;
                }
            }
            // Is a key values based collection.
            else if (genericArguments.Length >= 2)
            {
                foreach (var kvp in dynamicKeyValues)
                {
                    WriteProperty(kvp.Key);
                    InsertTabs();
                    WriteValue(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Writes the start of an object
        /// </summary>
        private void StartObject()
        {
            InsertNewLine();
            InsertTabs();
            _writer.Write((char)ControlCharacters.OpenBrace);
            InsertNewLine();

            _indentSize++;
        }

        /// <summary>
        /// Writes the end of an object
        /// </summary>
        private void EndObject()
        {
            _indentSize--;

            InsertNewLine();
            InsertTabs();
            _writer.Write((char)ControlCharacters.CloseBrace);
            InsertNewLine();
        }

        /// <summary>
        /// Insert the number of tabs to get the indent to the correct size
        /// </summary>
        private void InsertTabs()
        {
            for(int i = 0; i < _indentSize; i++)
            {
                _writer.Write((char)WhitespaceCharacters.Tab);
            }
        }

        /// <summary>
        /// Inserts characters to create a new line
        /// </summary>
        private void InsertNewLine()
        {
            _writer.Write((char)WhitespaceCharacters.CarriageReturn);
            _writer.Write((char)WhitespaceCharacters.NewLine);
        }

    }
}
