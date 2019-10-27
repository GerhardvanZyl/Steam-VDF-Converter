using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Xunit;

namespace VdfParser.Test
{

    public class DeserializerTests
    {
        [Fact]
        public void ParseSharedConfig()
        {
            FileStream sharedConfig = File.OpenRead("sharedconfig.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            dynamic result = parser.Deserialize(sharedConfig);

            Assert.Equal(result.UserRoamingConfigStore.Software.Valve.Steam.SurveyDate, "2017-07-03");

            var apps = result.UserRoamingConfigStore.Software.Valve.Steam.Apps as IDictionary<string, dynamic>;
            var tags = apps["251570"].tags as IDictionary<string, dynamic>;

            Assert.Equal(tags["0"], "Survival & Horror");
        }

        [Fact]
        public void ParseAndCastSimple()
        {
            FileStream sharedConfig = File.OpenRead("cast-test-basic.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            SteamBasic result = parser.Deserialize<SteamBasic>(sharedConfig);

            Assert.Equal("2586173360812765888", result.SurveyDateVersion);
            Assert.True(result.DesktopShortcutCheck);
        }

        [Fact]
        public void ParseAndCast()
        {
            FileStream sharedConfig = File.OpenRead("cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfFileTestExceprt result = parser.Deserialize<VdfFileTestExceprt>(sharedConfig);

            Assert.Equal("2586173360812765888", result.Steam.SurveyDateVersion);
            Assert.True(result.Steam.DesktopShortcutCheck);
            Assert.Equal("Strategy", result.Steam.Apps["434460"].Tags["1"]);
        }

        [Fact]
        public void ParseAndCastString()
        {
            string sharedConfig = File.ReadAllText("cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfFileTestExceprt result = parser.Deserialize<VdfFileTestExceprt>(sharedConfig);

            Assert.Equal("2586173360812765888", result.Steam.SurveyDateVersion);
            Assert.True(result.Steam.DesktopShortcutCheck);
            Assert.Equal("Strategy", result.Steam.Apps["434460"].Tags["1"]);
        }

    }
}
