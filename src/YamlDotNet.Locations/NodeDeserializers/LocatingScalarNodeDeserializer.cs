using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YamlDotNet.Locations.NodeDeserializers;

internal class LocatingScalarNodeDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer _inner;
    private readonly ParsingContext _context;

    public LocatingScalarNodeDeserializer(INodeDeserializer inner, ParsingContext context)
    {
        _inner = inner;
        _context = context;
    }

    public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
    {
        var previousParser = (LocatorParser)reader;
        var start = previousParser.Current.Start;
        
        var result = _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
        if (result)
            _context.AtScalar(start, previousParser.Previous.End, value);

        return result;
    }
}