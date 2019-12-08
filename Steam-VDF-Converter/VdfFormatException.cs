using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace VdfConverter
{
    public class VdfFormatException : Exception
    {
        public VdfFormatException() { }

        public VdfFormatException(string message) : base(message) { }

        public VdfFormatException(string message, Exception innerException) : base(message, innerException) { }

        public VdfFormatException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
