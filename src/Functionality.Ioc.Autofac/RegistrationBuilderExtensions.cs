#region LICENSE NOTICE

//! This file is subject to the terms and conditions defined in file 'LICENSE.md', which is part of this source code package.

#endregion

using Autofac;
using Autofac.Builder;

namespace Phoenix.Functionality.Ioc.Autofac;

/// <summary>
/// Contains extension methods for <see cref="IRegistrationBuilder{TLimit,TActivatorData,TRegistrationStyle}"/>.
/// </summary>
public static class RegistrationBuilderExtensions
{
	#region FindInternalConstructors

	/// <summary>
	/// A policy to also find internal constructors of the type.
	/// </summary>
	/// <typeparam name="TLimit"> Registration limit type. </typeparam>
	/// <typeparam name="TReflectionActivatorData"> Activator data type. </typeparam>
	/// <typeparam name="TStyle"> Registration style. </typeparam>
	/// <param name="registration"> Registration to set policy on. </param>
	/// <returns> A registration builder allowing further configuration of the component. </returns>
	public static IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> FindInternalConstructors<TLimit, TReflectionActivatorData, TStyle>
	(
		this IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> registration
	)
		where TReflectionActivatorData : ReflectionActivatorData
	{
		return registration.FindConstructorsWith(InternalConstructorFinder.Instance);
	}

	#endregion
}