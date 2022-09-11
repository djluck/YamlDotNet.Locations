using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Sprache;

namespace YamlDotNet.Locations;

public static class StringPathQueryExtensions
{
    private static readonly Parser<QueryMap> QueryPropertyParser = 
        from dot in Parse.Char('.').Once()
        from name in Parse.CharExcept(new [] {'[', ']', '.'}).AtLeastOnce().Text()
        select new QueryMap(name);
    
    private static readonly Parser<QueryMap> QueryDictionaryParser = 
        from open in Parse.Char('[').Once()
        from key in Parse.CharExcept(']').AtLeastOnce().Text()
        from close in Parse.Char(']').Once()
        select new QueryMap(key);
    
    private static readonly Parser<QuerySequence> QuerySequenceParser = 
        from open in Parse.Char('[').Once()
        from index in Parse.Numeric.AtLeastOnce().Text()
        from close in Parse.Char(']').Once()
        select new QuerySequence(int.Parse(index));
    
    private static readonly Parser<QueryValue> QueryValueParser = 
        from dot in Parse.Char('.').Once()
        select new QueryValue();

    private static readonly Parser<IQueryOp> QueryOpParser =
        QueryPropertyParser.Or(
            QuerySequenceParser.Or(
                QueryDictionaryParser.Or<IQueryOp>(
                    QueryValueParser
                )
            )
        );

    internal static readonly Parser<IEnumerable<IQueryOp>> QueryParser = QueryOpParser.AtLeastOnce();

    /// <summary>
    /// Queries locations of deserialized objects using dynamic path queries.
    /// </summary>
    /// <example>
    /// <code>
    /// // Returns location of root object
    /// locator.GetLocation(".")
    /// // Returns location of property Foo
    /// locator.GetLocation(".Foo")
    /// // Returns location of object at index 0 in a collection
    /// locator.GetLocation("[0]")
    /// // Returns location of object at key 'some_key' in a dictionary
    /// locator.GetLocation("[some_key]")
    /// // Returns location of object at index 0 in the collection Bar
    /// locator.GetLocation(".Bar[0]")
    /// // Returns location of dictionary key 'my_key'
    /// locator.GetLocation(".MyDictionary[my_key]")
    /// </code>
    /// </example>
    /// <returns></returns>
    public static LocationResult GetLocation<TDeserialized>(this ILocator<TDeserialized> locator, string queryPath)
    {
        return locator.GetLocation(QueryParser.Parse(queryPath).ToList());
    }
}