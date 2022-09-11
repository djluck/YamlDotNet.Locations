using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Locations.NodeDeserializers;
using YamlDotNet.Locations.TypeInspectors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace YamlDotNet.Locations;

public static class LocatingDeserializer
{
    public static (T deserialized , ILocator<T> locator) Deserialize<T>(this DeserializerBuilder builder, IParser parser)
    {
        var context = new ParsingContext();
        parser = new LocatorParser(parser);

        var yamlDeserializer = builder
            .WithNodeDeserializer(
                nd => new LocatingObjectNodeDeserializer(nd, context),
                x => x.InsteadOf<ObjectNodeDeserializer>())
            .WithNodeDeserializer(nd => new LocatingCollectionNodeDeserializer<CollectionNodeDeserializer>(nd, context ),
                 x => x.InsteadOf<CollectionNodeDeserializer>())
             .WithNodeDeserializer(nd => new LocatingDictionaryNodeDeserializer(nd, context ),
                 x => x.InsteadOf<DictionaryNodeDeserializer>())
            .WithNodeDeserializer(nd => new LocatingCollectionNodeDeserializer<ArrayNodeDeserializer>(nd, context ),
                x => x.InsteadOf<ArrayNodeDeserializer>())
            .WithNodeDeserializer(nd => new LocatingScalarNodeDeserializer(nd, context ),
                x => x.InsteadOf<ScalarNodeDeserializer>())
            .WithNodeDeserializer(nd => new LocatingTypeConverterNodeDeserializer(nd, context ),
                x => x.InsteadOf<TypeConverterNodeDeserializer>())
            .WithTypeInspector(nd => new PropertyNameLocatingInspector(nd, (LocatorParser)parser, context))
            .Build();
        ;
        
        var result = yamlDeserializer.Deserialize<T>(parser);
        return (result, new Locator<T>(context.CompleteParsing()));
    }
    
    public static (T deserialized , ILocator<T> locator) Deserialize<T>(this DeserializerBuilder builder, TextReader reader) =>
        Deserialize<T>(builder, new Parser(reader));
    
    public static (T deserialized , ILocator<T> locator) Deserialize<T>(this DeserializerBuilder builder, string s) =>
        Deserialize<T>(builder, new StringReader(s));
}