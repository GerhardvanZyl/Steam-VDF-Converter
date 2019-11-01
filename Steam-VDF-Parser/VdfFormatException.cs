using System;
using System.Collections.Generic;
using System.Text;

namespace VdfConverter
{
    public class VdfFormatException : Exception
    {
        public VdfFormatException(string message) : base(message)
        {

        }
    }
}
