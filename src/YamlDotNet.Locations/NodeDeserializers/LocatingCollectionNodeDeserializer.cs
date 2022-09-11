using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YamlDotNet.Locations.NodeDeserializers;

internal class LocatingCollectionNodeDeserializer<T> : INodeDeserializer
    where T : INodeDeserializer
{
    private readonly INodeDeserializer _inner;
    private readonly ParsingContext _context;

    public LocatingCollectionNodeDeserializer(INodeDeserializer inner, ParsingContext context)
    {
        _inner = inner;
        _context = context;
    }

    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        var previousParser = (LocatorParser)reader;
        _context.AtSequenceStart(previousParser.Current.Start);
        var success = _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
        
        if (success)
            _context.AtEnd(previousParser.Previous.End);
        else
            _context.NoMatch();

        return success;
    }
}