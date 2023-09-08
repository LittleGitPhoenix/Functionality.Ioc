using Autofac;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Phoenix.Functionality.Ioc.Autofac;

namespace Functionality.Ioc.Autofac.Test;

public class TypeAndFactoryListTest
{
	#region Setup

#pragma warning disable 8618 // → Always initialized in the 'Setup' method before a test is run.
	private IFixture _fixture;
#pragma warning restore 8618

	[OneTimeSetUp]
	public void BeforeAllTests() { }

	[SetUp]
	public void BeforeEachTest()
	{
		_fixture = new Fixture().Customize(new AutoMoqCustomization());
	}

	[TearDown]
	public void AfterEachTest() { }

	[OneTimeTearDown]
	public void AfterAllTests() { }

	#endregion

	#region Data

	interface ISomething { }

	class Anything_01 : ISomething { }
	
	class Anything_02 : ISomething { }

	class Anything_03 : Anything_01 { }
	
	class NotPart { }

	class Anything : ISomething
	{
		internal static int InstanceCount { get; private set; }

		public Anything()
		{
			InstanceCount++;
		}
	}

	#endregion

	#region Tests
	
	[Test]
	public void GetRegisteredTypesAndFactories()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterSource<TypeAndFactoryListSource<ISomething>>();
		builder.RegisterType<Anything_01>().AsSelf().As<ISomething>().SingleInstance();
		builder.RegisterType<Anything_02>().AsSelf().As<ISomething>().SingleInstance();
		builder.RegisterType<Anything_03>().AsSelf().As<ISomething>().SingleInstance();
		builder.RegisterType<NotPart>().SingleInstance();
		var container = builder.Build();

		// Act
		var typesAndFactories = container.Resolve<TypeAndFactoryList<ISomething>>();
		
		// Assert
		Assert.Multiple
		(
			() =>
			{
				Assert.NotNull(typesAndFactories);
				Assert.IsNotEmpty(typesAndFactories);
				Assert.That(typesAndFactories, Has.Count.EqualTo(3));

				var types = typesAndFactories.Select(tuple => tuple.Type).ToArray();
				Assert.That(types, Has.Member(typeof(Anything_01)));
				Assert.That(types, Has.Member(typeof(Anything_02)));
				Assert.That(types, Has.Member(typeof(Anything_03)));
				Assert.That(types, Has.No.Member(typeof(NotPart)));

				foreach (var (type, factory) in typesAndFactories)
				{
					var instance = factory.Invoke();
					Assert.That(instance, Is.TypeOf(type));
				}
			}
		);
	}

	[Test]
	public void NotRegisteringTypeAsSelfThrows()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterSource<TypeAndFactoryListSource<ISomething>>();
		builder.RegisterType<Anything_01>()/*.AsSelf()*/.As<ISomething>().SingleInstance(); //! Don't register AsSelf().
		var container = builder.Build();

		// Act
		var (_, factory) = container.Resolve<TypeAndFactoryList<ISomething>>().Single();
		var exception = Assert.Catch<global::Autofac.Core.Registration.ComponentNotRegisteredException>(() => factory.Invoke());
		
		// Assert
		Assert.That(exception, Is.Not.Null);
	}

	[Test]
	public void ObtainingTypeDoesNotCreateInstance()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterSource<TypeAndFactoryListSource<ISomething>>();
		builder.RegisterType<Anything>().AsSelf().As<ISomething>().SingleInstance();
		var container = builder.Build();

		// Act
		var types = container.Resolve<TypeAndFactoryList<ISomething>>();
		
		// Assert
		Assert.NotNull(types);
		Assert.IsNotEmpty(types);
		Assert.That(Anything.InstanceCount, Is.EqualTo(0));
	}

	#endregion
}