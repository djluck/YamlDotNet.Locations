using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Locations.NodeDeserializers;
using YamlDotNet.Locations.TypeInspectors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace YamlDotNet.Locations;

public static class LocatingDeserializer
{
    /// <summary>
    /// Deserializes a type complete with a <see cref="ILocator{TDeserialized}"/> that can be used to locate the positions
    /// of properties, collections and child objects within the deserialized object.
    /// </summary>
    /// <param name="parser"></param>
    /// <param name="configureBuilder">Optional. Allows additional configuration of the <see cref="DeserializerBuilder"/> before the deserializer is constructed.</param>
    /// <param name="maintainNamingConvention">If true, the YAML naming convention will be maintained and properties must be queried using this convention. Defaults to false.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static (T deserialized , ILocator<T> locator) Deserialize<T>(IParser parser, Func<DeserializerBuilder, ParsingContext, DeserializerBuilder>? configureBuilder = null, bool maintainNamingConvention = false)
    {
        configureBuilder ??= (builder, ctx) => builder;
        var context = new ParsingContext();
        parser = new LocatorParser(parser);

        var builder = new DeserializerBuilder();
        configureBuilder(builder, context);
        
        builder
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
            .WithTypeInspector(nd => new PropertyNameLocatingInspector(nd, (LocatorParser) parser, context, maintainNamingConvention));
                    
        var yamlDeserializer = builder.Build();

        var result = yamlDeserializer.Deserialize<T>(parser);
        return (result, new Locator<T>(context.CompleteParsing()));
    }
    
    public static (T deserialized , ILocator<T> locator) Deserialize<T>(TextReader reader, Func<DeserializerBuilder, ParsingContext, DeserializerBuilder>? configureBuilder = null, bool maintainNamingConvention = false) =>
        Deserialize<T>(new Parser(reader), configureBuilder, maintainNamingConvention);
    
    public static (T deserialized , ILocator<T> locator) Deserialize<T>(string s, Func<DeserializerBuilder, ParsingContext, DeserializerBuilder>? configureBuilder = null, bool maintainNamingConvention = false) =>
        Deserialize<T>(new StringReader(s), configureBuilder, maintainNamingConvention);
}