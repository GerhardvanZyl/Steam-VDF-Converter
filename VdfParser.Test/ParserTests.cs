using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Xunit;

namespace VdfParser.Test
{

    public class UnitTest1
    {
        [Fact]
        public void ParseSharedConfig()
        {
            FileStream sharedConfig = File.OpenRead("sharedconfig.vdf");

            VdfParser parser = new VdfParser();

            dynamic result = parser.Parse(sharedConfig);

            Assert.Equal(result.UserRoamingConfigStore.Software.Valve.Steam.SurveyDate, "2017-07-03");

            var apps = result.UserRoamingConfigStore.Software.Valve.Steam.Apps as IDictionary<string, dynamic>;
            var tags = apps["251570"].tags as IDictionary<string, dynamic>;

            Assert.Equal(tags["0"], "Survival & Horror");
        }
    }
}
