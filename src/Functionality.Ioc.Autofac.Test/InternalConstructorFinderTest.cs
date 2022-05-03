using Autofac;
using Autofac.Core.Activators.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Phoenix.Functionality.Ioc.Autofac;

namespace Functionality.Ioc.Autofac.Test;

public class InternalConstructorFinderTest
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

	class ClassWithInternalConstructor
	{
		internal ClassWithInternalConstructor() { }
	}

	#endregion

	#region Tests

	[Test]
	public void FindInternalConstructorSucceeds()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterType<ClassWithInternalConstructor>().AsSelf().FindInternalConstructors();
		var container = builder.Build();

		// Act
		var instance = container.Resolve<ClassWithInternalConstructor>();

		// Assert
		Assert.NotNull(instance);
	}

	[Test]
	public void FindInternalConstructorFails()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterType<ClassWithInternalConstructor>().AsSelf();
		
		// Act + Assert
		//! With Autofac v6 constructors are located at container build time, rather than the first time the component is resolved.
		//! So building will already throw.
		Assert.Catch<NoConstructorsFoundException>(() => builder.Build());
	}

	#endregion
}