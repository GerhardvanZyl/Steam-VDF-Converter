using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace VdfConverter
{
    public class VdfTypeException : Exception
    {
        public VdfTypeException() { }

        public VdfTypeException(string message) : base(message) { }

        public VdfTypeException(string message, Exception innerException) : base(message, innerException) { }

        public VdfTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
