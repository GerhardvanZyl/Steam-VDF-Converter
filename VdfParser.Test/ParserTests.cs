using System;
using System.IO;
using Xunit;
using VdfParser;

namespace VdfParser.Test
{
    public class UnitTest1
    {
        [Fact]
        public void ParseSharedConfig()
        {
            FileStream sharedConfig = File.OpenRead("sharedconfig.vdf");

            VdfParser parser = new VdfParser(sharedConfig);

            var result = parser.Parse();

            Assert.Equal(result.UserRoamingConfigStore.Software.Valve.Steam.SurveyDate, "2017-07-03");
          
        }
    }
}
