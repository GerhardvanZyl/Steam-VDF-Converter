# Steam-VDF-Converter
A C# parser for the Steam VDF file format.

# Installation
Nuget - [https://www.nuget.org/packages/VdfConverter/1.0.3](https://www.nuget.org/packages/VdfConverter/1.0.3)

```
Install-Package VdfConverter
```

# Example Usage

```c#
  FileStream testFile = File.OpenRead("test.vdf");
  VdfDeserializer deserializer = new VdfDeserializer();
  dynamic result = deserializer.Deserialize(testFile);
```
```c#
  FileStream testFile = File.OpenRead("test.vdf");
  VdfDeserializer deserializer = new VdfDeserializer();
  SteamGames resultObj = deserializer.Deserialize<SteamGames>(testFile);
```
```c#
  VdfSerializer serializer = new VdfSerializer();
  string result = serializer.Serialize(resultObj);
```

For more examples, as well as the VDF files test classes, look at the test project.
