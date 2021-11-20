using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Core;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Phoenix.Functionality.Ioc.Autofac;

namespace Functionality.Ioc.Autofac.Test
{
	public class NestedScopeBuilderTest
	{
#pragma warning disable 8618 // → Always initialized in the 'Setup' method before a test is run.
		private IFixture _fixture;
#pragma warning restore 8618

		[SetUp]
		public void Setup()
		{
			_fixture = new Fixture().Customize(new AutoMoqCustomization());
		}

		[Test]
		public async Task Check_Module_Will_Be_Added()
		{
			// Arrange
			var module = _fixture.Create<IModule>();
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(module).ToGroupAsync("MyGroup");

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(1));
		}

		[Test]
		public async Task Check_Modules_Will_Be_Added_To_Same_Group()
		{
			// Arrange
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("MyGroup");
			await builder.AddModules(_fixture.Create<IModule>(), _fixture.Create<IModule>()).ToGroupAsync("MyGroup");

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(3));
		}

		[Test]
		public async Task Check_Modules_Will_Be_Added_To_Different_Groups()
		{
			// Arrange
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("MyGroup");
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("OtherGroup");

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(2));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.Last().Modules, Has.Count.EqualTo(1));
		}

		/// <summary> Checks that an <see cref="IModule"/> can be added to a group even, if other groups have already been created. </summary>
		[Test]
		public async Task Check_Module_Can_Be_Added_To_Group_At_Any_Time()
		{
			// Arrange
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("MyGroup");
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("OtherGroup");
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("DifferentGroup");
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("MyGroup");

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(3));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(2));
		}

		/// <summary> Checks that an <see cref="IModule"/> can be added to a group even, if other groups have already been created. </summary>
		[Test]
		public async Task Check_Groups_Stay_In_Creation_Order()
		{
			// Arrange
			var group1 = "Group#01";
			var group2 = "Group#02";
			var group3 = "Group#03";
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(group1);
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(group2);
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(group1);
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(group3);
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(group3);
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(group2);

			// Assert
			Assert.That(builder.ModuleGroups[0].GroupName, Is.EqualTo(group1));
			Assert.That(builder.ModuleGroups[1].GroupName, Is.EqualTo(group2));
			Assert.That(builder.ModuleGroups[2].GroupName, Is.EqualTo(group3));
		}

		[Test]
		public async Task Check_Existing_Module_Will_Not_Be_Added()
		{
			// Arrange
			var module = _fixture.Create<IModule>();
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(module).ToGroupAsync("MyGroup");
			await builder.AddModule(module).ToGroupAsync("MyGroup");
			await builder.AddModule(module).ToGroupAsync("OtherGroup");

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(1));
		}

		/// <summary> Checks that the same <see cref="IModule"/> that was already added, can be added again if its new group is previous to its current one. The previous module will be deleted in this case. </summary>
		[Test]
		public async Task Check_Existing_Module_Will_Be_Added_To_Previous_Group()
		{
			// Arrange
			var module = _fixture.Create<IModule>();
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync("1st");
			await builder.AddModule(module).ToGroupAsync("2nd");
			await builder.AddModule(module).ToGroupAsync("1st");

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(2));
		}

		/// <summary> Checks that building a new scope resets the <see cref="NestedScopeBuilder"/> so it can be used again, with the new scope as starting point. </summary>
		[Test]
		public async Task Check_Building_New_Scope_Resets()
		{
			// Arrange
			var module = _fixture.Create<IModule>();
			var builder = new NestedScopeBuilder();
			await builder.AddModule(module).ToGroupAsync("MyGroup");

			// Act
			var scope = await builder.BuildAsync();

			// Assert
			Assert.IsEmpty(builder.ModuleGroups);
			Assert.AreSame(builder.Scope, scope);
		}

		/// <summary> Checks that adding an <see cref="IModule"/> before a group that not exists, will create the not existing group. </summary>
		[Test]
		public async Task Check_Adding_Module_Before_Not_Existing_Group_Succeeds()
		{
			// Arrange
			var groupName = Guid.NewGuid().ToString();
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).BeforeGroupAsync(groupName);

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(2));
			Assert.That(builder.ModuleGroups.First().GroupName, Is.EqualTo($"Before{groupName}"));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.Last().GroupName, Is.EqualTo(groupName));
			Assert.IsEmpty(builder.ModuleGroups.Last().Modules);
		}

		[Test]
		public async Task Check_Adding_Module_Before_Existing_Group_Succeeds()
		{
			// Arrange
			var groupName = Guid.NewGuid().ToString();
			var builder = new NestedScopeBuilder();
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(groupName);

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).BeforeGroupAsync(groupName);

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(2));
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(2));
			Assert.That(builder.ModuleGroups.First().GroupName, Is.EqualTo($"Before{groupName}"));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.Last().GroupName, Is.EqualTo(groupName));
			Assert.That(builder.ModuleGroups.Last().Modules, Has.Count.EqualTo(1));
		}

		/// <summary> Checks that adding an <see cref="IModule"/> after a group that not exists, will create the not existing group. </summary>
		[Test]
		public async Task Check_Adding_Module_After_Not_Existing_Group_Succeeds()
		{
			// Arrange
			var groupName = Guid.NewGuid().ToString();
			var builder = new NestedScopeBuilder();

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).AfterGroupAsync(groupName);

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(2));
			Assert.That(builder.ModuleGroups.First().GroupName, Is.EqualTo(groupName));
			Assert.IsEmpty(builder.ModuleGroups.First().Modules);
			Assert.That(builder.ModuleGroups.Last().GroupName, Is.EqualTo($"After{groupName}"));
			Assert.That(builder.ModuleGroups.Last().Modules, Has.Count.EqualTo(1));
		}

		[Test]
		public async Task Check_Adding_Module_After_Existing_Group_Succeeds()
		{
			// Arrange
			var groupName = Guid.NewGuid().ToString();
			var builder = new NestedScopeBuilder();
			await builder.AddModule(_fixture.Create<IModule>()).ToGroupAsync(groupName);

			// Act
			await builder.AddModule(_fixture.Create<IModule>()).AfterGroupAsync(groupName);

			// Assert
			Assert.That(builder.ModuleGroups, Has.Count.EqualTo(2));
			Assert.That(builder.ModuleGroups.First().GroupName, Is.EqualTo(groupName));
			Assert.That(builder.ModuleGroups.First().Modules, Has.Count.EqualTo(1));
			Assert.That(builder.ModuleGroups.Last().GroupName, Is.EqualTo($"After{groupName}"));
			Assert.That(builder.ModuleGroups.Last().Modules, Has.Count.EqualTo(1));
		}
	}
}