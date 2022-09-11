using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;

namespace YamlDotNet.Locations;

public interface IYamlNode
{
    public Mark Start { get; }
    public Mark? End { get; set; }
    internal void AddChild(IYamlNode child);
}

internal record MapEntry(Mark Start, Scalar Key) : IYamlNode
{
    public IYamlNode? Value { get; private set; }
    public Mark? End { get; set; }

    void IYamlNode.AddChild(IYamlNode child)
    {
        if (Value != null)
            throw new InvalidOperationException("May not mutate the child of a map entry once it is set");

        Value = child;
    }
}

internal record Map(Mark Start) : IYamlNode
{
    private readonly IDictionary<object, IYamlNode?> _map = new Dictionary<object, IYamlNode?>();
    private IYamlNode? _nextKey;

    public Mark? End { get; set; }
    
    public bool TryGet(object key, [NotNullWhen(true)] out IYamlNode? node) => _map.TryGetValue(key, out node);

    void IYamlNode.AddChild(IYamlNode child)
    {
        if (child is MapEntry me)
        {
            AddEntry(me.Key, me.Value);
            return;
        }
        if (_nextKey == null)
        {
            _nextKey = child;
            return;
        }
        
        AddEntry(_nextKey, child);
        _nextKey = null;
    }

    private void AddEntry(IYamlNode key, IYamlNode? value)
    {
        if (key is not Scalar scalar)
            throw new ArgumentException("Can only add scalars as mapping keys!");

        if (scalar.Value is null)
            throw new ArgumentException("null cannot be used for map keys!");

        _map.Add(scalar.Value, value);
    }
}

internal record Sequence(Mark Start) : IYamlNode
{
    private readonly List<IYamlNode> _items = new();
    
    public Mark? End { get; set; }
    
    public bool TryGet(int index, [NotNullWhen(true)] out IYamlNode? node)
    {
        if (index < _items.Count)
        {
            node = _items[index];
            return true;
        }
        
        node = default;
        return false;
    }

    void IYamlNode.AddChild(IYamlNode child) => _items.Add(child);
}

internal record Scalar(Mark Start, object? Value) : IYamlNode
{
    public Mark? End { get; set; }
    void IYamlNode.AddChild(IYamlNode child) => throw new NotImplementedException("Scalars cannot have children");
}

public class ParsingContext
{
    private IYamlNode? _root;
    
    public Stack<IYamlNode> Stack { get; set; } = new Stack<IYamlNode>();

    public void AtMapStart(Mark start)
    {
        var obj = new Map(start);
        Stack.Push(obj);
    }
    
    public void AtEnd(Mark end)
    {
        var obj = Stack.Pop();
        obj.End = end;
        
        if (!TryAddChild(obj))
            _root = obj;
    }

    public void AtSequenceStart(Mark start)
    {
        Stack.Push(new Sequence(start));
    }

    public void AtPropertyStart(Mark start, string name)
    {
        // TODO should we include positional information? Currently not used by anything
        Stack.Push(new MapEntry(start, new Scalar(Mark.Empty, name)));
    }
    
    public void AtScalar(Mark start, Mark end, object? value)
    {
        var scalar = new Scalar(start, value)
        {
            End = end
        };

        TryAddChild(scalar);
    }

    public void NoMatch() => Stack.Pop();

    private bool TryAddChild(IYamlNode child)
    {
        if (Stack.TryPeek(out var parent))
        {
            parent.AddChild(child);
            return true;
        }

        return false;
    }

    internal IYamlNode? CompleteParsing()
    {
        if (Stack.Count != 0)
            throw new InvalidOperationException($"Unexpected stack size of {Stack.Count}");
        
        return _root;
    }
}