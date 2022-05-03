using System.Net;
using Autofac;
using Autofac.Core;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Phoenix.Functionality.Ioc.Autofac;

namespace Functionality.Ioc.Autofac.Test;

public class ContainerBuilderExtensionsTest
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

	internal class ClassWithDelegateFactory
	{
		public delegate ClassWithDelegateFactory Factory(int id);

		public delegate ClassWithDelegateFactory WrongName(int id);

		public string Value { get; }

		public int Id { get; }

		public ClassWithDelegateFactory(string value, int id)
		{
			this.Value = value;
			this.Id = id;
		}
	}

	#endregion

	#region Tests

	#region RegisterFactory

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void RegisteringSucceeds()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterFactory<ClassWithDelegateFactory.Factory>();
		var container = builder.Build();

		// Act + Act
		Assert.DoesNotThrow(() => container.Resolve<ClassWithDelegateFactory.Factory>());
	}

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void ResolvingSucceeds()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterInstance(_fixture.Create<string>()).AsSelf().ExternallyOwned();
		builder.RegisterFactory<ClassWithDelegateFactory.Factory>();
		var container = builder.Build();

		// Act
		var factory = container.Resolve<ClassWithDelegateFactory.Factory>();
		var instance = factory.Invoke(_fixture.Create<int>());

		// Assert
		Assert.NotNull(factory);
		Assert.NotNull(instance);
	}

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void ResolvingWithParameterSucceeds()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder
			.RegisterFactory<ClassWithDelegateFactory.Factory>()
			.WithParameter
			(
				new global::Autofac.Core.ResolvedParameter
				(
					(pi, context) => pi.ParameterType == typeof(string),
					(pi, context) => _fixture.Create<string>()
				)
			)
			;
		var container = builder.Build();

		// Act
		var factory = container.Resolve<ClassWithDelegateFactory.Factory>();
		var instance = factory.Invoke(_fixture.Create<int>());

		// Assert
		Assert.NotNull(factory);
		Assert.NotNull(instance);
	}

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void ResolvingNamedSucceeds()
	{
		// Arrange
		var name = "MyDelegate";
		var wasUnnamedResolved = false;
		var wasNamedResolved = false;
		var builder = new ContainerBuilder();
		builder
			.RegisterFactory<ClassWithDelegateFactory.Factory>()
			.WithParameter
			(
				new global::Autofac.Core.ResolvedParameter
				(
					(pi, context) => pi.ParameterType == typeof(string),
					(pi, context) => "unnamed"
				)
			)
			.OnActivated(_ => wasUnnamedResolved = true)
			;
		builder
			.RegisterFactory<ClassWithDelegateFactory.Factory>()
			.Named<ClassWithDelegateFactory>(name)
			.WithParameter
			(
				new global::Autofac.Core.ResolvedParameter
				(
					(pi, context) => pi.ParameterType == typeof(string),
					(pi, context) => "named"
				)
			)
			.OnActivated(_ => wasNamedResolved = true)
			;
		var container = builder.Build();

		// Act
		var factory = container.ResolveNamed<ClassWithDelegateFactory.Factory>(name);
		var instance = factory.Invoke(_fixture.Create<int>());

		// Assert
		Assert.NotNull(factory);
		Assert.False(wasUnnamedResolved);
		Assert.True(wasNamedResolved);
		Assert.That(instance.Value, Is.EqualTo("named"));
	}

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void ThrowsIfBuilderIsNull()
	{
		// Act + Assert
		Assert.Catch<ArgumentNullException>(() => ContainerBuilderExtensions.RegisterFactory<ClassWithDelegateFactory.Factory>(null));
	}

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void ThrowsIfTypeIsNoDelegate()
	{
		// Arrange
		var builder = new ContainerBuilder();
		
		// Act + Assert
		Assert.Catch<ArgumentException>(() => builder.RegisterFactory<ClassWithDelegateFactory>());
	}

	[Test]
	[Category($"{nameof(ContainerBuilderExtensions.RegisterFactory)}")]
	public void ThrowsIfDelegateHasWrongName()
	{
		// Arrange
		var builder = new ContainerBuilder();
		
		// Act + Assert
		Assert.Catch<ArgumentException>(() => builder.RegisterFactory<ClassWithDelegateFactory.WrongName>());
	}

	#endregion

	#endregion
}