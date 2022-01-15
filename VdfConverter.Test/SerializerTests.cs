using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using VdfConverter;
using Xunit;

namespace VdfParser.Test
{
    public class SerializerTests
    {
        [Fact]
        public void Serialize_Dictionary()
        {
            FileStream sharedConfig = File.OpenRead("./InputFiles/cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfFileTestExceprt obj = parser.Deserialize<VdfFileTestExceprt>(sharedConfig);

            VdfSerializer serializer = new VdfSerializer();
            string result = serializer.Serialize(obj);

            File.WriteAllText(@"./result.txt", result);

            parser = new VdfDeserializer();

            VdfFileTestExceprt fullLoopDeserialized = parser.Deserialize<VdfFileTestExceprt>(result);

            Assert.Equal("2586173360812765888", fullLoopDeserialized.Steam.SurveyDateVersion);
            Assert.True(fullLoopDeserialized.Steam.DesktopShortcutCheck);
            Assert.Equal("Strategy", fullLoopDeserialized.Steam.Apps["434460"].Tags["1"]);
        }

        [Fact]
        public void Serialize_List()
        {
            FileStream sharedConfig = File.OpenRead("./InputFiles/cast-test.vdf");

            VdfDeserializer parser = new VdfDeserializer();

            VdfWithList obj = parser.Deserialize<VdfWithList>(sharedConfig);

            VdfSerializer serializer = new VdfSerializer();
            string result = serializer.Serialize(obj);

            parser = new VdfDeserializer();

            VdfWithList fullLoopDeserialized = parser.Deserialize<VdfWithList>(result);

            Assert.Equal("Strategy", fullLoopDeserialized.Steam.Apps["434460"].Tags[1]);
        }
    }
}
