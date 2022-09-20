using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YamlDotNet.Locations.TypeInspectors;

internal class PropertyNameLocatingInspector : ITypeInspector
{
	private ITypeInspector _inner;
	private LocatorParser _parser;
	private ParsingContext _context;
	private readonly bool _maintainNamingConvention;

	public PropertyNameLocatingInspector(ITypeInspector inner, LocatorParser parser, ParsingContext context,
		bool maintainNamingConvention)
	{
		_inner = inner;
		_parser = parser;
		_context = context;
		_maintainNamingConvention = maintainNamingConvention;
	}
	
	public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
	{
		return _inner.GetProperties(type, container);
	}

	public IPropertyDescriptor GetProperty(Type type, object? container, string name, [MaybeNullWhen(true)] bool ignoreUnmatched)
	{
		var p = _inner.GetProperty(type, container, name, ignoreUnmatched);

		// Certain parts of the YamlDotNet type inspector pipelines will transform property names, e.g. to match a naming convention
		// We need to access the underlying untransformed member name to be able to map member positions.
		var propertyName = _maintainNamingConvention ? p.Name : GetBaseDescriptor(p).Name;

		_context.AtPropertyStart(_parser.Current.Start, propertyName);

		return new WrappingPropertyDescriptor(p, _parser, _context);
	}

	private static IPropertyDescriptor GetBaseDescriptor(IPropertyDescriptor p)
	{
		while (p is PropertyDescriptor)
		{
			// TODO add scary warnings in README!
			// TODO Open MR in YamlDotNet to make PropertyDescriptor.baseDescriptor public
			// I feel so dirty doing this...
			var field = p.GetType().GetField("baseDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
				throw new MissingMemberException(
					$"Unable to find expected private field in the {nameof(PropertyDescriptor)} class! " +
					"This likely indicates you changed your YamlDotNet version and the library has altered an internal implementation detail we rely upon!. " +
					"Either revert your version change or report a bug at https://github.com/djluck/YamlDotNet.Locations."
				);
			p = ((IPropertyDescriptor) field.GetValue(p)!);
		}

		return p;
	}
	
	public class WrappingPropertyDescriptor : IPropertyDescriptor
	{
		private readonly IPropertyDescriptor _inner;
		private readonly LocatorParser _parser;
		private readonly ParsingContext _context;
		
		public WrappingPropertyDescriptor(IPropertyDescriptor toWrap, LocatorParser parser, ParsingContext context)
		{
			_inner = toWrap;
			_parser = parser;
			_context = context;
		}

		public string Name => _inner.Name;
		public bool CanWrite => _inner.CanWrite;
		public Type Type => _inner.Type;
		public Type? TypeOverride { get => _inner.TypeOverride; set => _inner.TypeOverride = value; }
		public int Order { get => _inner.Order; set => _inner.Order = value; }
		public ScalarStyle ScalarStyle { get => _inner.ScalarStyle; set => _inner.ScalarStyle = value; }

		public T GetCustomAttribute<T>() where T : Attribute
		{
			return _inner.GetCustomAttribute<T>();
		}

		public IObjectDescriptor Read(object target)
		{
			return _inner.Read(target);
		}

		public void Write(object target, object? value)
		{
			_context.AtEnd(_parser.Previous.End);
			_inner.Write(target, value);
		}
	}
}
