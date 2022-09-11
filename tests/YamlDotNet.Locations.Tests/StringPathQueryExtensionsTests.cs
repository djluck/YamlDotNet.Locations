using FluentAssertions;
using NUnit.Framework;
using Sprache;

namespace YamlDotNet.Locations.Tests;

[TestFixture]
public class StringPathQueryExtensionsTests
{
    [Test]
    public void Can_Query_Current_Value()
    {
        StringPathQueryExtensions.QueryParser.Parse(".")
            .Should()
            .Equal(new QueryValue());
    }
    
    [Test]
    public void Can_Query_Object_Property()
    {
        StringPathQueryExtensions.QueryParser.Parse(".Hello")
            .Should()
            .Equal(new QueryMap("Hello"));
    }
    
    [Test]
    public void Can_Query_Dictionary()
    {
        StringPathQueryExtensions.QueryParser.Parse("[some_key]")
            .Should()
            .Equal(new QueryMap("some_key"));
    }
    
    [Test]
    public void Can_Query_Collection()
    {
        StringPathQueryExtensions.QueryParser.Parse("[12]")
            .Should()
            .Equal(new QuerySequence(12));
    }
    
    [Test]
    public void Can_Query_Complex()
    {
        StringPathQueryExtensions.QueryParser.Parse(".SomeProp[1].SomeOtherProp[2][3]")
            .Should()
            .Equal(
                new QueryMap("SomeProp"),
                new QuerySequence(1),
                new QueryMap("SomeOtherProp"),
                new QuerySequence(2),
                new QuerySequence(3)
            );
    }
}