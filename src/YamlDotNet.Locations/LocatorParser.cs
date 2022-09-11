using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace YamlDotNet.Locations;

 /// <summary>
/// As the YAML parser is advanced to the next unconsumable event for a <see cref="INodeDeserializer"/>, in order to generate
/// accurate location information we need a parser that can track the last successfully consumed event. 
/// </summary>
internal class LocatorParser : IParser
{
    private readonly IParser _wrapped;

    internal LocatorParser(IParser wrapped)
    {
        _wrapped = wrapped;
    }

    public bool MoveNext()
    {
        Previous = Current;
        EventsRead++;
        return _wrapped.MoveNext();
    }

    [AllowNull]
    public ParsingEvent Previous { get; private set; }
    public ParsingEvent Current => _wrapped.Current!;
    public int EventsRead { get; private set; }
}