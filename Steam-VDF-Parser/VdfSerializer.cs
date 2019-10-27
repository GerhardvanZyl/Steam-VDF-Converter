using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VdfParser.Enums;
using VdfParser.Test;

namespace VdfConverter
{
    public class VdfSerializer
    {
        private TextWriter _writer;
        private int _indentSize;

        public VdfSerializer()
        {
        }

        public string Serialize(object content)
        {
            using (_writer = new StringWriter())
            {
                WriteProperties(content);
            }

            return _writer.ToString();
        }

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

        private void WriteProperty(string property)
        {
            InsertTabs();
            _writer.Write("\"");
            _writer.Write(property);
            _writer.Write("\"");
            _writer.Write((char)WhitespaceCharacters.Tab);
        }

        private void WriteValue(object value)
        {
            _writer.Write((char)WhitespaceCharacters.Tab);
            
            Type valueType = value.GetType();

            if (valueType == typeof(string))
            {
                _writer.Write("\"");
                _writer.Write(value);
                _writer.Write("\"");

                _writer.Write((char)WhitespaceCharacters.CarriageReturn);
                _writer.Write((char)WhitespaceCharacters.NewLine);

            }
            else if (valueType == typeof(bool))
            {
                _writer.Write("\"");
                _writer.Write(value.ToString().ToLowerInvariant() == "true" ? "1" : "0"); // Hacky way to try parse and default to false (0)
                _writer.Write("\"");

                _writer.Write((char)WhitespaceCharacters.CarriageReturn);
                _writer.Write((char)WhitespaceCharacters.NewLine);
            }
            else if (valueType == typeof(DateTime))
            {
                _writer.Write("\"");
                _writer.Write(value.ToString());
                _writer.Write("\"");

                _writer.Write((char)WhitespaceCharacters.CarriageReturn);
                _writer.Write((char)WhitespaceCharacters.NewLine);
            }
            else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                StartObject();
                WriteDictionary(value);
                EndObject();
            }
            else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
            {
                StartObject();
                WriteList(value);
                EndObject();
            }
            else // another object
            {
                StartObject();
                WriteProperties(value);
                EndObject();
            }
        }

        private void WriteDictionary(object keyValues)
        {
            Type dictionaryType = typeof(Dictionary<string, object>);

            // For some reason I can't do keyValues as Dictionary(string, object). Probably because the type is not referenced
            var keys = dictionaryType
                .GetProperties()
                .Where(
                    x => x.Name == "Keys"
                );

            dynamic dynamicKeyValues = (dynamic)keyValues;

            foreach (var kvp in dynamicKeyValues)
            {
                WriteProperty(kvp.Key);
                InsertTabs();
                WriteValue(kvp.Value);
            }
        }

        private void WriteList(object values)
        {
            Type listType = typeof(List<object>);

            dynamic listValues = (dynamic)values;

            foreach (var obj in listValues)
            {
                InsertTabs();
                // TODO handle non strings
                WriteValue((string)obj);

            }
        }

        private void StartObject()
        {
            _writer.Write((char)WhitespaceCharacters.CarriageReturn);
            _writer.Write((char)WhitespaceCharacters.NewLine);
            InsertTabs();
            _writer.Write((char)ControlCharacters.OpenBrace);
            _writer.Write((char)WhitespaceCharacters.CarriageReturn);
            _writer.Write((char)WhitespaceCharacters.NewLine);
            
            _indentSize++;
        }

        private void EndObject()
        {
            _indentSize--;

            _writer.Write((char)WhitespaceCharacters.CarriageReturn);
            _writer.Write((char)WhitespaceCharacters.NewLine);
            InsertTabs();
            _writer.Write((char)ControlCharacters.CloseBrace);
            _writer.Write((char)WhitespaceCharacters.CarriageReturn);
            _writer.Write((char)WhitespaceCharacters.NewLine);
        }

        private void InsertTabs()
        {
            for(int i = 0; i < _indentSize; i++)
            {
                _writer.Write((char)WhitespaceCharacters.Tab);
            }
        }

    }
}
