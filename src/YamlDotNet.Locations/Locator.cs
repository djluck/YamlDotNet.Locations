using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
/// <param name="ExecutedOperations">Contains the query operations that we successfully executed. When <see cref="IsSuccess"/> is true, this is equal to all of the query operations specified.</param>
/// <param name="LastLocation">If <see cref="IsSuccess"/> is false, contains the last successfully resolved location. Can be null when <see cref="ExecutedOperations"/> is empty.</param>
public record struct LocationResult(Location? Location, ImmutableArray<IQueryOp> ExecutedOperations, string? ErrorMessage, Location? LastLocation = null)
{
    public LocationResult(Location location, ImmutableArray<IQueryOp> executedOperations) : this(location, executedOperations, null) {}
    public LocationResult(string errorMessage, ImmutableArray<IQueryOp> executedOperations, Location? lastLocation) : this(null, executedOperations, errorMessage, lastLocation) {}

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
        
        return $"Location query failed: {ErrorMessage}. {GetAttemptedOps()}";
    }
    
    private string GetAttemptedOps() =>
        $"Executed query operations: {(ExecutedOperations.Length == 0 ? "<none>" : string.Join(" -> ", ExecutedOperations))}";
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
            $"Query did not match deserialized structure, expected {failedOp.GetType().Name.Replace("Query", "")} but found {node.GetType().Name}"
        );

        if (query.Count < 1)
            throw new ArgumentException("Must provide at least one query operation", nameof(query));
        
        IYamlNode? current = _root;
        IYamlNode? last = null;
        if (current == null)
            return new LocationResult("Deserialized object was null", ImmutableArray<IQueryOp>.Empty, null);

        var executed = new List<IQueryOp>();
        foreach (var op in query)
        {
            var next = op switch
            {
                QuerySequence(int index) => 
                    current is Sequence s ? 
                        s.TryGet(index, out var node) ? 
                            new OpResult(node) : 
                            new OpResult($"The requested sequence index '{index}' did not exist") 
                        : GetMismatchedStructureResult(op, current),
                
                QueryMap(object key) => 
                    current is Map m ?
                        m.TryGet(key, out var node) ?
                            new OpResult(node) :
                            new OpResult($"The requested map key '{key}' did not exist")
                        : GetMismatchedStructureResult(op, current),
                
                QueryValue => new OpResult(current),
                _ => throw new InvalidOperationException($"Unsupported query op '{op}'")
            };

            last = current;
            
            if (!next.IsSuccess)
                return new LocationResult(next.ErrorMessage, executed.ToImmutableArray(), last == null ? null : ToLocation(last));
            
            executed.Add(op);
            current = next.Node;
        }

        return new LocationResult(ToLocation(current), executed.ToImmutableArray());
    }

    private static Location ToLocation(IYamlNode n)
    {
        if (!n.End.HasValue)
            throw new InvalidOperationException("Node was partially located!");
        
        return new Location(n.Start, n.End.Value);
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