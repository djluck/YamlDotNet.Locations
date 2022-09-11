using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace YamlDotNet.Locations.NodeDeserializers;

internal class LocatingObjectNodeDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer _inner;
    private readonly ParsingContext _context;

    public LocatingObjectNodeDeserializer(INodeDeserializer inner, ParsingContext context)
    {
        _inner = inner;
        _context = context;
    }

    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        var previousParser = (LocatorParser)reader;
        _context.AtMapStart(previousParser.Current.Start);
        var success = _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
        
        if (success)
            _context.AtEnd(previousParser.Previous.End);
        else
            _context.NoMatch();

        return success;
    }
}