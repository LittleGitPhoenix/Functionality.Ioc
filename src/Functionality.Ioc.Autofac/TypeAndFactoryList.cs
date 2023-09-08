#region LICENSE NOTICE

//! This file is subject to the terms and conditions defined in file 'LICENSE.md', which is part of this source code package.

#endregion

using Autofac;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;

namespace Phoenix.Functionality.Ioc.Autofac;

/// <summary>
/// Collection that can be used to get all registered types (as opposed to instances) from <see cref="Autofac"/> together with a factory method to create instance of the types.
/// </summary>
/// <typeparam name="T"> The type to get from <see cref="Autofac"/>. </typeparam>
public class TypeAndFactoryList<T> : List<(Type Type, Func<T> Factory)>
{
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="typesAndFactories"> The types alongside factory methods to create instances of the type. </param>
	public TypeAndFactoryList(IEnumerable<(Type Type, Func<T> Factory)> typesAndFactories) : base(typesAndFactories) { }
}

/// <summary>
/// <see cref="IRegistrationSource"/> used to resolve <see cref="TypeAndFactoryList{T}"/>.
/// </summary>
/// <typeparam name="T"> The type to get from <see cref="Autofac"/>. </typeparam>
public class TypeAndFactoryListSource<T> : IRegistrationSource
{
	/// <inheritdoc />
	public bool IsAdapterForIndividualComponents => false;

	/// <inheritdoc />
	public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
	{
		if (service is not IServiceWithType serviceWithType || !typeof(TypeAndFactoryList<T>).IsAssignableFrom(serviceWithType.ServiceType)) return Enumerable.Empty<IComponentRegistration>();

		var registration = new ComponentRegistration
		(
			id: Guid.NewGuid(),
			activator: new DelegateActivator
			(
				serviceWithType.ServiceType, (context, parameters) =>
				{
					var typesAndFactories = context
						.ComponentRegistry
						.RegistrationsFor(new TypedService(typeof(T)))
						.Select(registration => registration.Activator)
						.OfType<ReflectionActivator>()
						.Select
						(
							activator =>
							{
								//if (!context.IsRegistered(activator.LimitType)) throw new ComponentNotRegisteredException(service, new Exception($"When registering services that should be accessed via {nameof(TypeAndFactoryList<T>)}, it is necessary to also register the service as self, so individual instances of it can be created."));
								var resolver = context.Resolve<IComponentContext>();
								return (activator.LimitType, (Func<T>) Factory);
								T Factory() => (T) resolver.Resolve(activator.LimitType);
							}
						)
						;
					return new TypeAndFactoryList<T>(typesAndFactories);
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