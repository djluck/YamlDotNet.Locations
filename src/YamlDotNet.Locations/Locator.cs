using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using YamlDotNet.Core;

namespace YamlDotNet.Locations;

/// <summary>
/// Provides ability to translate queries over a structure deserialized from YAML into <see cref="Location"/>s.
/// </summary>
/// <typeparam name="TDeserialized"></typeparam>
public interface ILocator<TDeserialized>
{ 
    public LocationResult GetLocation(ICollection<IQueryOp> query);
}

/// <summary>
/// </summary>
/// <param name="Location">If <see cref="IsSuccess"/> is true, contains the location of the requested object.</param>
/// <param name="ErrorMessage">If <see cref="IsSuccess"/> is false, contains an error message describing why the query failed.</param>
public record struct LocationResult(Location? Location, string? ErrorMessage)
{
    public LocationResult(Location location) : this(location, null) {}
    public LocationResult(string errorMessage) : this(null, errorMessage) {}

    /// <summary>
    /// Returns true if a location was successfully found.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Location))]
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    public bool IsSuccess => Location != null;

    public override string ToString()
    {
        if (IsSuccess)
            return Location.ToString();
        
        return $"Location query failed: {ErrorMessage}";
    }
}

public record Location(Mark Start, Mark End)
{
    public override string ToString() => $"({Start.Line}:{Start.Column})-({End.Line}:{End.Column})";
}

internal class Locator<TDeserialized> : ILocator<TDeserialized>
{
    private readonly IYamlNode? _root;

    public Locator(IYamlNode? root)
    {
        _root = root;
    }

    LocationResult ILocator<TDeserialized>.GetLocation(ICollection<IQueryOp> query)
    {
        OpResult GetMismatchedStructureResult(IQueryOp failedOp, IYamlNode node) => new OpResult(
            $"Query did not match deserialized structure, expected {failedOp.GetType().Name.Replace("Query", "")} but found {node.GetType().Name}. "
            + GetExecutedOps(failedOp)
        );

        string GetExecutedOps(IQueryOp failedOp)
        {
            var successfulOps = query.TakeWhile(x => x != failedOp).Select(x => x.ToString()).ToArray();
            return $"Executed query operations: {(successfulOps.Length == 0 ? "<none>" : string.Join(" -> ", successfulOps))}";
        }

        if (query.Count < 1)
            throw new ArgumentException("Must provide at least one query operation", nameof(query));
        
        IYamlNode? current = _root;
        if (current == null)
            return new LocationResult("Deserialized object was null");
        
        foreach (var op in query)
        {
            var next = op switch
            {
                QuerySequence(int index) => 
                    current is Sequence s ? 
                        s.TryGet(index, out var node) ? 
                            new OpResult(node) : 
                            new OpResult($"The requested sequence index '{index}' did not exist! " + GetExecutedOps(op)) 
                        : GetMismatchedStructureResult(op, current),
                
                QueryMap(object key) => 
                    current is Map m ?
                        m.TryGet(key, out var node) ?
                            new OpResult(node) :
                            new OpResult($"The requested map key '{key}' did not exist! " + GetExecutedOps(op))
                        : GetMismatchedStructureResult(op, current),
                
                QueryValue => new OpResult(current),
                _ => throw new InvalidOperationException($"Unsupported query op '{op}'")
            };

            if (!next.IsSuccess)
                return new LocationResult(next.ErrorMessage);
            
            current = next.Node;
        }

        if (!current.End.HasValue)
            throw new InvalidOperationException("Node was partially located!");

        return new LocationResult(new Location(current.Start, current.End.Value));
    }

    private record struct OpResult(IYamlNode? Node, string? ErrorMessage)
    {
        public OpResult(IYamlNode node) : this(node, null) {}
        public OpResult(string errorMessage) : this(null, errorMessage) {}
        
        [MemberNotNullWhen(true, nameof(Node))]
        [MemberNotNullWhen(false, nameof(ErrorMessage))]
        public bool IsSuccess => Node != null;
    }
}

public interface IQueryOp {}

/// <summary>
/// Queries a sequence structure (e.g. array, IEnumerable, etc), navigating to the specified index.
/// </summary>
/// <param name="AtIndex"></param>
public record QuerySequence(int AtIndex) : IQueryOp
{
    public override string ToString() => $"Sequence[{AtIndex}]";
}

/// <summary>
/// Queries a map structure (e.g. dictionary or object), navigating to the specified key.
/// </summary>
/// <param name="AtKey"></param>
public record QueryMap(object AtKey) : IQueryOp
{
    public override string ToString() => $"Map[{AtKey}]";
}

/// <summary>
/// Returns the location of the current map, sequence or scalar.
/// </summary>
public record QueryValue() : IQueryOp
{
    public override string ToString() => $"Value";
}