using System;
using NUnit.Framework;

namespace YamlDotNet.Locations.Tests;

[TestFixture]
public class LinqQueryExtensionsTests
{
    private Locator<SampleClass> _locator = new Locator<SampleClass>(null);

    [Test]
    public void null_Throws_Exception()
    {
        Assert.Throws<ArgumentException>(() => _locator.GetLocation(x => (string) null));
    }
    
    [Test]
    public void Binary_Expr_Throws_Exception()
    {
        Assert.Throws<ArgumentException>(() => _locator.GetLocation(x => 1 + 1));
    }
    
    [Test]
    public void Method_Calls_Throws_Exception()
    {
        Assert.Throws<ArgumentException>(() => _locator.GetLocation(x => x.MyProperty.Clone()));
    }

    public class SampleClass
    {
        public string MyProperty { get; }
    }
}