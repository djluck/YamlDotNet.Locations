﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YamlDotNet.Locations.Tests;

[TestFixture]
public class LocatorTests
{
    [TestCase]
    public void Cannot_Locate_null_Object()
    {
        var toParse = @"
---
 null
";

        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation(x => x).IsSuccess.Should().BeFalse();
    }
    
    [TestCase()]
    public void Can_Locate_Empty_Object()
    {
        var toParse = @"
---
 { }
";

        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation(x => x).ToString()
            .Should().Be("(3:2)-(3:4)");
    }
    
    [TestCase()]
    public void Can_Locate_Object_Property_Values()
    {
        var toParse = @"
prop_one: test
prop_twooo: 1
";

        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation(x => x.PropOne).ToString()
            .Should().Be("(2:11)-(2:15)");
        
        locator.GetLocation(x => x.PropTwooo).ToString()
            .Should().Be("(3:13)-(3:14)");
    }
    
    [TestCase]
    public void Can_Locate_Aliased_Object_Property_Value()
    {
        var toParse = @"
my_overriden_property_name: test
";

        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation(x => x.Overriden).ToString()
            .Should().Be("(2:29)-(2:33)");
    }
    
    [TestCase()]
    public void Can_Locate_TypeConverter_Property_Values()
    {
        var toParse = @"
a_time_span: 2.10:00:05
a_guid: 13294ef6-baa4-4ab6-9801-8fca3848948f
a_date_time: 2022-09-10T00:00:00Z
";

        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation(x => x.ATimeSpan).ToString()
            .Should().Be("(2:14)-(2:24)");
        
        locator.GetLocation(x => x.AGuid).ToString()
            .Should().Be("(3:9)-(3:45)");
        
        locator.GetLocation(x => x.ADateTime).ToString()
            .Should().Be("(4:14)-(4:34)");
    }
    
    [TestCase]
    public void Can_Locate_Collection_Elements()
    {
        var toParse = @"
- onee
- ""2""
";

        var locator = Deserialize<List<string>>(toParse);
        locator.GetLocation(x => x[0]).ToString()
            .Should().Be("(2:3)-(2:7)");
        locator.GetLocation(x => x[1]).ToString()
            .Should().Be("(3:3)-(3:6)");
    }
    
    [TestCase]
    public void Can_Locate_Array_Elements()
    {
        var toParse = @"
- onee
- ""2""
";

        var locator = Deserialize<string[]>(toParse);
        locator.GetLocation(x => x[0]).ToString()
            .Should().Be("(2:3)-(2:7)");
        locator.GetLocation(x => x[1]).ToString()
            .Should().Be("(3:3)-(3:6)");
    }
    
    [TestCase]
    public void Can_Locate_Enumerable_Elements()
    {
        var toParse = @"
- onee
- ""2""
";

        var locator = Deserialize<IEnumerable<string>>(toParse);
        locator.GetLocation(x => x.ElementAt(0)).ToString()
            .Should().Be("(2:3)-(2:7)");
        locator.GetLocation(x => x.ElementAt(1)).ToString()
            .Should().Be("(3:3)-(3:6)");
    }
    
    [TestCase()]
    public void Can_Locate_Dictionary_Values_When_Keys_Are_Not_string()
    {
        var toParse = @"
true: test
";

        var locator = Deserialize<Dictionary<bool, string>>(toParse);
        locator.GetLocation(x => x[true]).ToString()
            .Should().Be("(2:7)-(2:11)");
    }
    
    [TestCase()]
    public void Can_Locate_Dictionary_Of_Dictionaries()
    {
        var toParse = @"
outer:
  a: 1
  b: 2
";

        var locator = Deserialize<Dictionary<string, Dictionary<string, int>>>(toParse);
        locator.GetLocation(x => x["outer"]).ToString()
            .Should().Be("(3:3)-(5:1)");
        
        locator.GetLocation(x => x["outer"]["b"]).ToString()
            .Should().Be("(4:6)-(4:7)");
    }
    
    [TestCase()]
    public void Can_Locate_Collections_Of_Scalar_Collections()
    {
        var toParse = @"
- 
  - one
  - two
- 
  - three
";

        var locator = Deserialize<List<string[]>>(toParse);
        locator.GetLocation(x => x).ToString()
            .Should().Be("(2:1)-(7:1)");
        locator.GetLocation(x => x[0]).ToString()
            .Should().Be("(3:3)-(5:1)");
        locator.GetLocation(x => x[0][0]).ToString()
            .Should().Be("(3:5)-(3:8)");
        locator.GetLocation(x => x[0][1]).ToString()
            .Should().Be("(4:5)-(4:8)");
        locator.GetLocation(x => x[1][0]).ToString()
            .Should().Be("(6:5)-(6:10)");
    }
    
    [TestCase()]
    public void Can_Locate_Collections_Of_Objects()
    {
        var toParse = @"
- prop_one: 1
  prop_twooo: 2
- prop_one: 3
  prop_twooo: 4
";

        var locator = Deserialize<SampleClass[]>(toParse);
        locator.GetLocation(x => x[0].PropOne).ToString()
            .Should().Be("(2:13)-(2:14)");
        locator.GetLocation(x => x[0].PropTwooo).ToString()
            .Should().Be("(3:15)-(3:16)");
        locator.GetLocation(x => x[1].PropOne).ToString()
            .Should().Be("(4:13)-(4:14)");
        locator.GetLocation(x => x[1].PropTwooo).ToString()
            .Should().Be("(5:15)-(5:16)");
    }
    
    [TestCase()]
    public void Can_Maintain_Naming_Conventions()
    {
        var toParse = @"
- prop_one: 1
";

        var locator = Deserialize<SampleClass[]>(toParse, maintainNamingConvention: true);
        locator.GetLocation(".[0].prop_one").ToString()
            .Should().Be("(2:13)-(2:14)");
    }
    
    [TestCase()]
    public void Can_Locate_On_Unary_Expression()
    {
        var toParse = @"
prop_twooo: 1
prop_three: zero
";

        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation<SampleClass, object>(x => x.PropThree).ToString()
            .Should().Be("(3:13)-(3:17)");
        locator.GetLocation<SampleClass, object>(x => x.PropTwooo).ToString()
            .Should().Be("(2:13)-(2:14)");
        locator.GetLocation<SampleClass, object>(x => (object)x.PropTwooo).ToString()
            .Should().Be("(2:13)-(2:14)");
    }
    
    
    [TestCase()]
    public void Can_handle_index_closures()
    {
        var toParse = @"
prop_list:
  - 0
  - 1
  - 2
  - 3
  - 4
";
        int idx = 2;
        Func<int, int> indexGen = (index) => index;
        var locator = Deserialize<SampleClass>(toParse);
        locator.GetLocation<SampleClass, object>(x => x.PropList[2]).ToString()
            .Should().Be("(5:5)-(5:6)");
        locator.GetLocation(x => x.PropList[idx]).ToString()
            .Should().Be("(5:5)-(5:6)");
        locator.GetLocation<SampleClass, object>(ScopedIndex()).ToString()
            .Should().Be("(5:5)-(5:6)");
        locator.GetLocation(x => x.PropList[indexGen(2)]).ToString()
            .Should().Be("(5:5)-(5:6)");
        
    }

    [TestCase()]
    public void Can_handle_key_closures()
    {
        var toParse = @"
collection:
  key1 : value1
  key2 : value2
";
        var locator = Deserialize<SampleClass>(toParse);
        var key = "key2";

        locator.GetLocation<SampleClass, object>(x => x.Collection["key1"]).ToString()
            .Should().Be("(3:10)-(3:16)");
        locator.GetLocation<SampleClass, object>(x => x.Collection[key]).ToString()
            .Should().Be("(4:10)-(4:16)");
        locator.GetLocation<SampleClass, object>(ScopedKey()).ToString()
            .Should().Be("(4:10)-(4:16)");
    }

    private Expression<Func<SampleClass, object>> ScopedIndex()
    {
        int idx = 2;
        return s => s.PropList[idx];
    }

    private Expression<Func<SampleClass, object>> ScopedKey()
    {
        string key = "key2";
        return s => s.Collection[key];
    }
    
    public ILocator<T> Deserialize<T>(string yaml, bool maintainNamingConvention = false)
    {
        return LocatingDeserializer.Deserialize<T>(yaml, (builder, ctx) => builder.WithNamingConvention(UnderscoredNamingConvention.Instance), maintainNamingConvention)
            .locator;
    }

    public enum SampleEnum
    {
        Zero,
        One,
        Two
    }

    public class Base
    {
        public SampleEnum PropThree { get; set; }
    }
    public class SampleClass: Base
    {
        public string PropOne { get; set; }
        public int PropTwooo { get; set; }
        
        [YamlMember(Alias = "MyOverridenPropertyName")]
        public string Overriden { get; set; }
        
        public Guid AGuid { get; set; }
        public TimeSpan ATimeSpan { get; set; }
        public DateTime ADateTime { get; set; }
        public List<int> PropList { get; set; }
        
        public Dictionary<string, string> Collection { get; set; }
    }
}

[TestFixture]
public class LocatorErrorHandlingTests
{
    private ILocator<SampleClass> _locator;

    [SetUp]
    public void SetUp()
    {
        var toParse = @"
collection:
  - one
  - two
property: hello!
";

        _locator = Deserialize<SampleClass>(toParse);
    }

    [Test]
    public void Empty_Query_Throws_Exception()
    {
        Assert.Throws<ArgumentException>(() => _locator.GetLocation(new List<IQueryOp>()));
    }
    
    [Test]
    public void Collection_Query_Over_Object_Fails()
    {
        var result = _locator.GetLocation(new List<IQueryOp>()
        {
            new QuerySequence(1)
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expected Sequence but found Map");
        result.ExecutedOperations.Should().BeEmpty();
        result.LastLocation.ToString().Should().Be("(2:1)-(6:1)");
    }
    
    [Test]
    public void Object_Query_Over_Value_Fails()
    {
        var result = _locator.GetLocation(new List<IQueryOp>()
        {
            new QueryMap("Collection"),
            new QuerySequence(0),
            new QueryMap("NonExistentProperty")
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expected Map but found Scalar");
        result.ToString().Should().Contain("Executed query operations: Map[Collection] -> Sequence[0]");
        result.ExecutedOperations.Should().Equal(new QueryMap("Collection"), new QuerySequence(0));
        result.LastLocation.ToString().Should().Be("(3:5)-(3:8)");
    }
    
    [Test]
    public void Collection_Out_Of_Bounds_Fails()
    {
        var result = _locator.GetLocation(new List<IQueryOp>()
        {
            new QueryMap("Collection"),
            new QuerySequence(3),
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("The requested sequence index '3' did not exist");
        result.ExecutedOperations.Should().Equal(new QueryMap("Collection"));
        result.LastLocation.ToString().Should().Be("(3:3)-(5:1)");
    }
    
    [Test]
    public void Non_Existent_Map_Key_Fails()
    {
        var result = _locator.GetLocation(new List<IQueryOp>()
        {
            new QueryMap("IDontExist"),
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("The requested map key 'IDontExist' did not exist");
        result.ExecutedOperations.Should().BeEmpty();
        result.LastLocation.ToString().Should().Be("(2:1)-(6:1)");
    }
    
    [Test]
    public void Object_Query_Over_Collection_Fails()
    {
        var result = _locator.GetLocation(new List<IQueryOp>()
        {
            new QueryMap("Collection"),
            new QueryMap("NonExistentProperty")
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expected Map but found Sequence");
        result.ToString().Should().Contain("Executed query operations: Map[Collection]");
        result.ExecutedOperations.Should().Equal(new QueryMap("Collection"));
        result.LastLocation.ToString().Should().Be("(3:3)-(5:1)");
    }

    private ILocator<T> Deserialize<T>(string yaml)
    {
        return LocatingDeserializer.Deserialize<T>(yaml, (builder, ctx) => builder.WithNamingConvention(UnderscoredNamingConvention.Instance))
            .locator;
    }

    public class SampleClass
    {
        public string[] Collection { get; set; }
        public string Property { get; set; }
    }
}