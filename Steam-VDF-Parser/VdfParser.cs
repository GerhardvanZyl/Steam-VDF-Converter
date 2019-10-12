using System;
using System.IO;
using System.Text;

namespace VdfParser
{
    public class VdfParser
    {
        // If we dont use a using, how does this affect memory?
        private StreamReader vdfFileReader;

        public VdfParser(Stream input) {
            vdfFileReader = new StreamReader(input);
        }

        VdfParser(string input)
        {
            throw new NotImplementedException();
        }

        public dynamic Parse()
        {
            throw new NotImplementedException();
        }

        private string GetNextProperty()
        {
            char cStart = (char)vdfFileReader.Read();
            bool isQuoted = cStart.Equals('"');

            bool cont = true;
            StringBuilder sb = new StringBuilder();
            do
            {
                char c = (char)vdfFileReader.Read();

                if ((isQuoted && c.Equals('"')) || c.Equals(' '))
                {
                    cont = false;
                }
                else
                {
                    sb.Append(c.ToString());
                }
            }
            while (cont);

            return sb.ToString();
        }

    }
}
