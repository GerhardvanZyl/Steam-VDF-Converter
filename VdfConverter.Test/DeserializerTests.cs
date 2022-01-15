using System;
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
            FileStream sharedConfig = File.OpenRead("./InputFiles/sharedconfig.vdf");

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
            FileStream sharedConfig = File.OpenRead("./InputFiles/cast-test-basic.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            SteamBasic result = parser.Deserialize<SteamBasic>(sharedConfig);

            Assert.Equal("2586173360812765888", result.SurveyDateVersion);
            Assert.True(result.DesktopShortcutCheck);
        }

        [Fact]
        public void ParseAndCast()
        {
            FileStream sharedConfig = File.OpenRead("./InputFiles/cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfFileTestExceprt result = parser.Deserialize<VdfFileTestExceprt>(sharedConfig);

            Assert.Equal("2586173360812765888", result.Steam.SurveyDateVersion);
            Assert.True(result.Steam.DesktopShortcutCheck);
            Assert.Equal("Strategy", result.Steam.Apps["434460"].Tags["1"]);
        }

        [Fact]
        public void ParseAndCastWithList()
        {
            FileStream sharedConfig = File.OpenRead("./InputFiles/cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfWithList result = parser.Deserialize<VdfWithList>(sharedConfig);

            Assert.Equal("Strategy", result.Steam.Apps["434460"].Tags[1]);
        }

        [Fact]
        public void ParseAndCastString()
        {
            string sharedConfig = File.ReadAllText("./InputFiles/cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfFileTestExceprt result = parser.Deserialize<VdfFileTestExceprt>(sharedConfig);

            Assert.Equal("2586173360812765888", result.Steam.SurveyDateVersion);
            Assert.True(result.Steam.DesktopShortcutCheck);
            Assert.Equal("Strategy", result.Steam.Apps["434460"].Tags["1"]);
        }
        [Fact]
        public void ParseAndCastCustomDictionary()
        {
            string sharedConfig = File.ReadAllText("./InputFiles/cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            CustomVdfExcerpt result = parser.Deserialize<CustomVdfExcerpt>(sharedConfig);

            Assert.NotNull(result.Steam.Apps["434460"]);
        }

        [Fact]
        public void ParseEscapedString()
        {
            string sharedConfig = File.ReadAllText("./InputFiles/escaped-quotes.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            var result = parser.Deserialize(sharedConfig);

            Assert.Equal("Ричард написал статью, которую одобрили сторонники \"«Переломного момента»\".", result.desc.russian);
        }

        [Fact]
        public void ParseDictionaryWithExtraProperty()
        {
            string sharedConfig = File.ReadAllText("./InputFiles/dictionary-extra-prop.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            Library result = parser.Deserialize<Library>(sharedConfig);

            Assert.Equal(2, result.libraryfolders.Count);
            Assert.Equal("-1882263624787241560", result.libraryfolders.contentstatsid);
        }

        [Fact]
        public void ParseDictionaryWithMismatchedType_Error()
        {
            string sharedConfig = File.ReadAllText("./InputFiles/dictionary-mismatch-type.vdf");
            VdfDeserializer parser = new VdfDeserializer();

            Assert.ThrowsAny<Exception>(() => 
            { 
                Library result = parser.Deserialize<Library>(sharedConfig);
                
                Console.WriteLine(result.libraryfolders.contentstatsid);
            });
        }

        [Fact]
        public void ParseDictionaryWithMismatchedType_IgnoreTypeMismatch()
        {
            string sharedConfig = File.ReadAllText("./InputFiles/dictionary-mismatch-type.vdf");
            VdfDeserializer parser = new VdfDeserializer(true);

            Library result = parser.Deserialize<Library>(sharedConfig);
            
            Assert.Single(result.libraryfolders);
        }
    }
}
