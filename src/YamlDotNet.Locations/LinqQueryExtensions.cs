using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace YamlDotNet.Locations;

public static class LinqQueryExtensions
{
    private const string SupportedOperationsExplanation =
        "Ensure your expression is composed only of object references (e.g. x => x), " +
        "collection indexers (e.g. x => x.MyCollection[1]), " +
        "enumerable element accessor methods (e.g. x => x.ElementAt(1)), " +
        "dictionary accessors (e.g. x => x.MyDictionary[\"some_key\"]) or " +
        "property references (e.g. x => x.MyProperty)";
    
    /// <summary>
    /// Queries locations of deserialized objects using type-safe LINQ expressions.
    /// </summary>
    /// <example>
    /// <code>
    /// // Returns location of root object
    /// locator.GetLocation(x => x)
    /// // Returns location of property Foo
    /// locator.GetLocation(x => x.Foo)
    /// // Returns location of object at index 0 in the collection Bar
    /// locator.GetLocation(x => x.Bar[0])
    /// // Returns location of object at index 0 in the IEnumerable Baz
    /// locator.GetLocation(x => x.Baz.ElementAt(0))
    /// // Returns location of dictionary key 'my_key' in the Dictionary MyDictionary
    /// locator.GetLocation(x => x.MyDictionary["my_key"])
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException"></exception>
    public static LocationResult GetLocation<TDeserialized, TVal>(this ILocator<TDeserialized> locator,
        Expression<Func<TDeserialized, TVal>> memberExpression)
    {
        static IEnumerable<IQueryOp> GetQueryFromExpression(Expression<Func<TDeserialized, TVal>> memberExpression)
        {
            Expression? expr = memberExpression.Body;

            while (expr != null)
            {
                if (expr is ParameterExpression)
                {
                    expr = null;
                    yield return new QueryValue();
                }
                else if (expr is MemberExpression m)
                {
                    expr = m.Expression;
                    yield return new QueryMap(m.Member.Name);
                }
                else if (expr is BinaryExpression {NodeType: ExpressionType.ArrayIndex} b)
                {
                    var index = (int) (b.Right as ConstantExpression)!.Value!;
                    expr = b.Left;
                    yield return new QuerySequence(index);
                }
                else if (expr is MethodCallExpression method)
                {
                    expr = method.Object;
                    // Support for indexing arrays
                    if (method.Arguments.Count == 1 && method.Method.Name == "get_Item")
                    {
                        var parameters = method.Method.GetParameters();
                        if (parameters[0].ParameterType == typeof(int))
                        {
                            var index = (int) (method.Arguments[0] as ConstantExpression)!.Value!;
                            yield return new QuerySequence(index);
                        }
                        else
                        {
                            var key = (method.Arguments[0] as ConstantExpression)!.Value!;
                            yield return new QueryMap(key);
                        }
                    }
                    // Support for indexing enumeraables
                    else if (method.Arguments.Count == 2 && method.Method.Name == "ElementAt")
                    {
                        var index = (int) (method.Arguments[1] as ConstantExpression)!.Value!;
                        yield return new QuerySequence(index);
                    }
                    else
                        throw new ArgumentException($"Method call '{method.Method}' is not supported! " + SupportedOperationsExplanation);
                }
                else
                    throw new ArgumentException($"Expression '{expr}' is not supported! " + SupportedOperationsExplanation);
            }
        }

        return locator.GetLocation(
            // As LINQ expression parsing works from left to right, we need to reverse the query operations
            GetQueryFromExpression(memberExpression).Reverse().ToList()
        );
    }
}