# YamlDotNet.Locations
An extension of the [YamlDotNet](https://github.com/aaubry/YamlDotNet) library that allows the user to resolve deserialized objects to their position within the source YAML.

## User guide
### Installation and requirements 
Requires .NET 6.0 and YamlDotNet 12.0. Install with nuget:
```
dotnet add YamlDotNet.Locations
```

### Locating deserialized objects
#### Using LINQ expressions
LINQ expressions can be used as a typesafe method to query the locations of deserialized values, e.g:
```csharp
var yaml = @"---
Object:
  Prop1: hello you
  Collection:
    - one
    - two 
"
// MyClass definition omitted for brevity
var (deserialiedValue, locator) = LocatingDeserializer.Deserialize<MyClass>(yaml);

// Output: (2:1)-(7:1)
Console.WriteLine(locator.GetLocation(x => x));
// Output: (3:3)-(7:1)
Console.WriteLine(locator.GetLocation(x => x.Object));
// Output: (3:10)-(3:19)
Console.WriteLine(locator.GetLocation(x => x.Object.Prop1));
// Output: (5:5)-(7:1)
Console.WriteLine(locator.GetLocation(x => x.Object.Collection));
// Output: (6:7)-(6:10)
Console.WriteLine(locator.GetLocation(x => x.Object.Collection[1]));
```
#### Using string-based queries
String based LINQ-style query expressions can also be used:
```csharp
var yaml = @"---
Object:
  Prop1: hello you
  Collection:
    - one
    - two 
";

// MyClass definition omitted for brevity
var (deserialiedValue, locator) = LocatingDeserializer.Deserialize<MyClass>(yaml);

// Output: (2:1)-(7:1)
Console.WriteLine(locator.GetLocation( "."));
// Output: (3:3)-(7:1)
Console.WriteLine(locator.GetLocation( ".Object"));
// Output: (3:10)-(3:19)
Console.WriteLine(locator.GetLocation(".Object.Prop1"));
// Output: (5:5)-(7:1)
Console.WriteLine(locator.GetLocation(".Object.Collection"));
// Output: (6:7)-(6:10)
Console.WriteLine(locator.GetLocation(".Object.Collection[1]"));
```

## A Dire Warning
This library works by exploiting the internal implementation details of YamlDotNet. While it has good test coverage, if these implementation
details change in the future, it may stop working correctly. 
