using Autofac;
using Autofac.Core;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Phoenix.Functionality.Ioc.Autofac;

namespace Functionality.Ioc.Autofac.Test;

public class TypeListSourceTest
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
	public void Check_Get_Registered_Types()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterSource<TypeListSource<ISomething>>();
		builder.RegisterType<Anything_01>().As<ISomething>().SingleInstance();
		builder.RegisterType<Anything_02>().As<ISomething>().SingleInstance();
		builder.RegisterType<Anything_03>().As<ISomething>().SingleInstance();
		builder.RegisterType<NotPart>().SingleInstance();
		var container = builder.Build();

		// Act
		var types = container.Resolve<TypeList<ISomething>>();
		
		// Assert
		Assert.Multiple
		(
			() =>
			{
				Assert.NotNull(types);
				Assert.IsNotEmpty(types);
				Assert.That(types, Has.Count.EqualTo(3));
				Assert.That(types, Has.Member(typeof(Anything_01)));
				Assert.That(types, Has.Member(typeof(Anything_02)));
				Assert.That(types, Has.Member(typeof(Anything_03)));
				Assert.That(types, Has.No.Member(typeof(NotPart)));
			}
		);
	}

	[Test]
	public void Check_Obtaining_Type_Does_Not_Create_Instance()
	{
		// Arrange
		var builder = new ContainerBuilder();
		builder.RegisterSource<TypeListSource<ISomething>>();
		builder.RegisterType<Anything>().As<ISomething>().SingleInstance();
		var container = builder.Build();

		// Act
		var types = container.Resolve<TypeList<ISomething>>();
		
		// Assert
		Assert.NotNull(types);
		Assert.IsNotEmpty(types);
		Assert.That(Anything.InstanceCount, Is.EqualTo(0));
	}

	#endregion
}