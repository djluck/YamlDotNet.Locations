using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YamlDotNet.Locations.NodeDeserializers;

internal class LocatingTypeConverterNodeDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer _inner;
    private readonly ParsingContext _context;

    public LocatingTypeConverterNodeDeserializer(INodeDeserializer inner, ParsingContext context)
    {
        _inner = inner;
        _context = context;
    }
    
    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        var previousParser = (LocatorParser)reader;
        var start = previousParser.Current.Start;
        var lastEventsRead = previousParser.EventsRead;
        var result = _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
        
        // This isn't perfect but should work for most cases as type converters are mostly used to convert custom string representations
        // into objects. If more than 1 event was read, we can't assume it'll be safe to record this as a scalar so ignore.
        // TODO how can we add support to ITypeConverters that deserialize more exotic structures?
        if (result && previousParser.EventsRead - lastEventsRead == 1)
            _context.AtScalar(start, previousParser.Previous.End, value);

        return result;
    }
}