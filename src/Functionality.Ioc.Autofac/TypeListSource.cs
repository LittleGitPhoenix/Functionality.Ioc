#region LICENSE NOTICE

//! This file is subject to the terms and conditions defined in file 'LICENSE.md', which is part of this source code package.

#endregion

using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Activators.Reflection;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;

namespace Phoenix.Functionality.Ioc.Autofac;

/// <summary>
/// Collection that can be used to get all registered types (as opposed to instances) from <see cref="Autofac"/>.
/// </summary>
/// <typeparam name="T"> The type to get from <see cref="Autofac"/>. </typeparam>
public class TypeList<T> : List<Type>
{
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="types"> The types. </param>
	public TypeList(IEnumerable<Type> types) : base(types) { }
}

/// <summary>
/// <see cref="IRegistrationSource"/> used to resolve <see cref="TypeList{T}"/>.
/// </summary>
/// <typeparam name="T"> The type to get from <see cref="Autofac"/>. </typeparam>
public class TypeListSource<T> : IRegistrationSource
{
	/// <inheritdoc />
	public bool IsAdapterForIndividualComponents => false;

	/// <inheritdoc />
	public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
	{
		if (service is not IServiceWithType serviceWithType || !typeof(TypeList<T>).IsAssignableFrom(serviceWithType.ServiceType)) return Enumerable.Empty<IComponentRegistration>();

		var registration = new ComponentRegistration
		(
			id: Guid.NewGuid(),
			activator: new DelegateActivator
			(
				serviceWithType.ServiceType, (context, parameters) =>
				{
					var types = context
						.ComponentRegistry
						.RegistrationsFor(new TypedService(typeof(T)))
						.Select(registration => registration.Activator)
						.OfType<ReflectionActivator>()
						.Select(activator => activator.LimitType)
						;
					return new TypeList<T>(types);
				}
			),
			services: new[] { service },
			lifetime: new CurrentScopeLifetime(),
			sharing: InstanceSharing.None,
			ownership: InstanceOwnership.OwnedByLifetimeScope,
			metadata: new Dictionary<string, object?>()
		);
		return new IComponentRegistration[] { registration };
	}
}