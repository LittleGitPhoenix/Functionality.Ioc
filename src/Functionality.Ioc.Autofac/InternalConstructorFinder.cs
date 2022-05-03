#region LICENSE NOTICE

//! This file is subject to the terms and conditions defined in file 'LICENSE.md', which is part of this source code package.

#endregion

using System.Collections.Concurrent;
using System.Reflection;
using Autofac.Core.Activators.Reflection;

namespace Phoenix.Functionality.Ioc.Autofac;

/// <summary>
/// Special <see cref="IConstructorFinder"/> that is able to find constructors that are internal.
/// </summary>
public class InternalConstructorFinder : IConstructorFinder
{
	#region Delegates / Events
	#endregion

	#region Constants
	#endregion

	#region Fields
	
	private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> Cache;
	
	#endregion

	#region Properties

	/// <summary>
	/// Singleton instance.
	/// </summary>
	public static InternalConstructorFinder Instance => LazyInstance.Value;

	private static readonly Lazy<InternalConstructorFinder> LazyInstance = new Lazy<InternalConstructorFinder>(() => new InternalConstructorFinder());

	#endregion

	#region (De)Constructors

	static InternalConstructorFinder()
	{
		Cache = new();
	}

	/// <summary>
	/// Private constructor to enforce singleton.
	/// </summary>
	private InternalConstructorFinder() { }

	#endregion

	#region Methods
	
	/// <inheritdoc />
	public ConstructorInfo[] FindConstructors(Type targetType)
	{
		var result = Cache.GetOrAdd
		(
			targetType,
			type => type.GetTypeInfo().DeclaredConstructors.Where(constructor => !constructor.IsStatic && !constructor.IsPrivate).ToArray()
		);

		return result.Length > 0 ? result : throw new NoConstructorsFoundException(targetType);
	}

	#endregion
}