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
		var delegateType = typeof(TFactory);
		if (!typeof(System.Delegate).IsAssignableFrom(delegateType)) throw new ArgumentException($"The generic type argument '{nameof(TFactory)}' is of type '{delegateType.FullName}' but must be of type '{typeof(System.Delegate).FullName}'.");
		
		var declaringType = delegateType.DeclaringType;
		if (declaringType is null) throw new ArgumentException($"The delegate '{delegateType.FullName}' is standalone but must be declared within another type to be used as delegate factory.");
		
		if (delegateType.Name != "Factory") throw new ArgumentException($"The generic type argument '{nameof(TFactory)}' of type '{delegateType.FullName}' must be named 'Factory'.");
		var returnType = delegateType.GetMethod("Invoke")!.ReturnType;

		// Check if the return type of the delegate can be assigned to the type containing the delegate.
		if (!returnType.IsAssignableFrom(declaringType)) throw new ArgumentException($"the delegate factories '{delegateType.FullName}' return type '{returnType.FullName}' must be assignable from the type '{delegateType.FullName}' it is declared in.");

		// Register the declaring type, so that Autofac can pick-up the delegate factory.
		return builder.RegisterType(declaringType).AsSelf();
	}

	#endregion
}