#region LICENSE NOTICE

//! This file is subject to the terms and conditions defined in file 'LICENSE.md', which is part of this source code package.

#endregion

using Autofac;
using Autofac.Builder;

namespace Phoenix.Functionality.Ioc.Autofac;

/// <summary>
/// Contains extension methods for <see cref="ContainerBuilder"/>.
/// </summary>
public static class ContainerBuilderExtensions
{
	#region RegisterFactory

	/// <summary>
	/// Registers the return type of a delegate factory as itself.
	/// </summary>
	/// <typeparam name="TFactory"></typeparam>
	/// <param name="builder"> Container builder. </param>
	/// <returns> Registration builder allowing the registration to be configured. </returns>
	/// <exception cref="ArgumentException">
	/// <para> Thrown if: </para>
	/// <para> • <paramref name="builder"/> is null. </para>
	/// <para> • <typeparamref name="TFactory"/> is not a <see cref="System.Delegate"/>. </para>
	/// <para> • <typeparamref name="TFactory"/> is not named <b>Factory</b>. </para>
	/// </exception>
	/// <remarks> This is basically the same a calling <b>builder.RegisterType(returnType).AsSelf()</b> with the difference, that it is now possible to identify registrations that are used to resolve factories. </remarks>
	public static IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterFactory<TFactory>(this ContainerBuilder builder)
		where TFactory : notnull
	{
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(builder);
#else
		if (builder == null) throw new ArgumentNullException(nameof(builder));
#endif

		// Get the return type of the delegate.
		var implementerType = typeof(TFactory);
		if (!typeof(System.Delegate).IsAssignableFrom(implementerType)) throw new ArgumentException($"The generic type argument '{nameof(TFactory)}' is of type '{implementerType.FullName}' but must be of type '{typeof(System.Delegate).FullName}'.");
		if (implementerType.Name != "Factory") throw new ArgumentException($"The generic type argument '{nameof(TFactory)}' of type '{implementerType.FullName}' must be named 'Factory'.");
		var returnType = implementerType.GetMethod("Invoke")!.ReturnType;

		// Register the return type.
		return builder.RegisterType(returnType).AsSelf();
	}

	#endregion
}