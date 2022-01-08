#region LICENSE NOTICE
//! This file is subject to the terms and conditions defined in file 'LICENSE.md', which is part of this source code package.
#endregion


using Autofac;
using Autofac.Core;

namespace Phoenix.Functionality.Ioc.Autofac;

/// <summary>
/// Helper interface implemented by <see cref="NestedScopeBuilder"/> so the builder pattern can used to formulate module and group relationships.
/// </summary>
public interface INestedScopeBuilderModuleAdder
{
	/// <summary>
	/// Adds a single <see cref="IModule"/>.
	/// </summary>
	/// <typeparam name="TModule"> The type of the module to add. </typeparam>
	/// <returns> An <see cref="INestedScopeBuilderGroupSpecifier"/> for chaining. </returns>
	INestedScopeBuilderGroupSpecifier AddModule<TModule>() where TModule : IModule, new();

	/// <summary>
	/// Adds the <paramref name="module"/>.
	/// </summary>
	/// <param name="module"> The <see cref="IModule"/> to add. </param>
	/// <returns> An <see cref="INestedScopeBuilderGroupSpecifier"/> for chaining. </returns>
	INestedScopeBuilderGroupSpecifier AddModule(IModule module);

	/// <summary>
	/// Adds all <paramref name="modules"/>.
	/// </summary>
	/// <param name="modules"> A collection of <see cref="IModule"/>s to add. </param>
	/// <returns> An <see cref="INestedScopeBuilderGroupSpecifier"/> for chaining. </returns>
	INestedScopeBuilderGroupSpecifier AddModules(params IModule[] modules);
}

/// <summary>
/// Helper interface implemented by <see cref="NestedScopeBuilder"/> so the builder pattern can used to formulate module and group relationships.
/// </summary>
public interface INestedScopeBuilderGroupSpecifier : INestedScopeBuilderModuleAdder
{
	/// <summary>
	/// Conflates all previously added <see cref="IModule"/>s with the given <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"> The name of the group. </param>
	/// <returns> An <see cref="INestedScopeBuilder"/> for chaining. </returns>
	INestedScopeBuilder ToGroup(string groupName);

	/// <summary>
	/// Conflates all previously added <see cref="IModule"/>s with the given <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"> The name of the group. </param>
	/// <param name="cancellationToken"> Optional <see cref="CancellationToken"/>. </param>
	/// <returns> An <see cref="INestedScopeBuilder"/> for chaining. </returns>
	Task<INestedScopeBuilder> ToGroupAsync(string groupName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Conflates all previously added <see cref="IModule"/>s with the given <paramref name="groupName"/> and inserts it as a new group right before the one with the given <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"> All previously added modules will be added to a new group directly before the one identified by this name. </param>
	/// <returns> An <see cref="INestedScopeBuilder"/> for chaining. </returns>
	INestedScopeBuilder BeforeGroup(string groupName);

	/// <summary>
	/// Conflates all previously added <see cref="IModule"/>s with the given <paramref name="groupName"/> and inserts it as a new group right before the one with the given <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"> All previously added modules will be added to a new group directly before the one identified by this name. </param>
	/// <param name="cancellationToken"> Optional <see cref="CancellationToken"/>. </param>
	/// <returns> An <see cref="INestedScopeBuilder"/> for chaining. </returns>
	Task<INestedScopeBuilder> BeforeGroupAsync(string groupName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Conflates all previously added <see cref="IModule"/>s with the given <paramref name="groupName"/> and inserts it as a new group right after the one with the given <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"> All previously added modules will be added to a new group directly after the one identified by this name. </param>
	/// <returns> An <see cref="INestedScopeBuilder"/> for chaining. </returns>
	INestedScopeBuilder AfterGroup(string groupName);

	/// <summary>
	/// Conflates all previously added <see cref="IModule"/>s with the given <paramref name="groupName"/> and inserts it as a new group right after the one with the given <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"> All previously added modules will be added to a new group directly after the one identified by this name. </param>
	/// <param name="cancellationToken"> Optional <see cref="CancellationToken"/>. </param>
	/// <returns> An <see cref="INestedScopeBuilder"/> for chaining. </returns>
	Task<INestedScopeBuilder> AfterGroupAsync(string groupName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Helper interface implemented by <see cref="NestedScopeBuilder"/> so the builder pattern can used to formulate module and group relationships.
/// </summary>
public interface INestedScopeBuilder : INestedScopeBuilderGroupSpecifier
{
	/// <summary>
	/// Builds all nested scopes according to the previously added groups and returns the last created scope.
	/// </summary>
	/// <param name="cancellationToken"> Optional <see cref="CancellationToken"/>. </param>
	/// <returns> The last created child <see cref="ILifetimeScope"/>. </returns>
	/// <remarks> This can be called multiple times. Each time all added modules are used to created a new scope that then will be used as a new staring base. </remarks>
	Task<ILifetimeScope> BuildAsync(/*Action<string>? logCallback = null, */CancellationToken cancellationToken = default);
}

/// <summary>
/// Allows to build nested <see cref="ILifetimeScope"/>s each build from a single group consisting of multiple <see cref="IModule"/>s.
/// </summary>
/// <remarks> This class basically helps to dynamically setup a <see cref="ILifetimeScope"/>-chain used by an application. </remarks>
public class NestedScopeBuilder : INestedScopeBuilder
{
	#region Delegates / Events
	#endregion

	#region Constants
	#endregion

	#region Fields

	/// <summary> Lock mechanism that block adding more modules during build and verification. </summary>
	private readonly SemaphoreSlim _buildLock;

	private readonly List<IModule> _modules;

	#endregion

	#region Properties

	/// <summary> The current <see cref="ILifetimeScope"/>. </summary>
	/// <remarks> This will be updated each time <see cref="INestedScopeBuilder.BuildAsync"/> is called. </remarks>
	public ILifetimeScope Scope { get; set; }

	internal IList<ModuleGroup> ModuleGroups { get; }
		
	#endregion

	#region (De)Constructors

	/// <summary>
	/// Constructor where the initial <see cref="ILifetimeScope"/> is empty.
	/// </summary>
	public NestedScopeBuilder() : this(new ContainerBuilder().Build()) { }

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="modules"> A collection of <see cref="IModule"/>s used to build the initial <see cref="ILifetimeScope"/>. </param>
	public NestedScopeBuilder(params IModule[] modules) : this
	(
		((Func<ILifetimeScope>) delegate()
		{
			var builder = new ContainerBuilder();
			foreach (var module in modules)
			{
				builder.RegisterModule(module);
			}
			return builder.Build();
		})()
	) { }

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="initialScope"> The initial <see cref="ILifetimeScope"/> used as starting point to build nested scopes. </param>
	public NestedScopeBuilder(ILifetimeScope initialScope)
	{
		// Save parameters.
		this.Scope = initialScope;

		// Initialize fields.
		_buildLock = new SemaphoreSlim(1, 1);
		_modules = new List<IModule>();
		this.ModuleGroups = new List<ModuleGroup>();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public INestedScopeBuilderGroupSpecifier AddModule<TModule>() where TModule : IModule, new()
		=> this.AddModules(new TModule());

	/// <inheritdoc />
	public INestedScopeBuilderGroupSpecifier AddModule(IModule module)
		=> this.AddModules(module);

	/// <inheritdoc />
	public INestedScopeBuilderGroupSpecifier AddModules(params IModule[] modules)
	{
		_modules.AddRange(modules);
		return this;
	}

	/// <inheritdoc />
	public INestedScopeBuilder ToGroup(string groupName) => this.ToGroupAsync(groupName).Result;

	/// <inheritdoc />
	public async Task<INestedScopeBuilder> ToGroupAsync(string groupName, CancellationToken cancellationToken = default)
	{
		await this.AddModulesToGroupAsync(groupName, cancellationToken, _modules.ToArray()).ConfigureAwait(false);
		_modules.Clear();
		return this;
	}

	/// <inheritdoc />
	public INestedScopeBuilder BeforeGroup(string groupName) => this.BeforeGroupAsync(groupName).Result;

	/// <inheritdoc />
	public Task<INestedScopeBuilder> BeforeGroupAsync(string groupName, CancellationToken cancellationToken = default)
	{
		// Check if the new before-group has to be inserted.
		var beforeGroupName = $"Before{groupName}";
		var index = this.ModuleGroups.IndexOf(groupName);
		if (index < 0)
		{
			this.ModuleGroups.Add(beforeGroupName);
			this.ModuleGroups.Add(groupName);
		}
		if (index >= 0)
		{
			this.ModuleGroups.Insert(index, beforeGroupName);
		}

		return this.ToGroupAsync(beforeGroupName, cancellationToken);
	}

	/// <inheritdoc />
	public INestedScopeBuilder AfterGroup(string groupName) => this.AfterGroupAsync(groupName).Result;

	/// <inheritdoc />
	public Task<INestedScopeBuilder> AfterGroupAsync(string groupName, CancellationToken cancellationToken = default)
	{
		// Check if the new after-group has to be inserted.
		var afterGroupName = $"After{groupName}";
		var index = this.ModuleGroups.IndexOf(groupName);
		if (index < 0)
		{
			this.ModuleGroups.Add(groupName);
			this.ModuleGroups.Add(afterGroupName);
		}
		if (index > 0 && index <= this.ModuleGroups.Count)
		{
			this.ModuleGroups.Insert(index, afterGroupName);
		}

		return this.ToGroupAsync(afterGroupName, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<ILifetimeScope> BuildAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			await _buildLock.WaitAsync(cancellationToken);
				
			var parentScope = this.Scope;
			foreach (var (_, modules) in this.ModuleGroups)
			{
				// Create a new scope.
				parentScope = NestedScopeBuilder.CreateChildScope(parentScope, modules);
			}
				
			// Clear all existing modules and set the initial scope to the last created one. This enables this instance to be reused.
			this.ModuleGroups.Clear();
			this.Scope = parentScope;

			// Return the final scope.
			return parentScope;
		}
		finally
		{
			_buildLock.Release();
		}
	}

	#region Helper

	private async Task AddModulesToGroupAsync(string groupName, CancellationToken cancellationToken = default, params IModule[] modules)
	{
		try
		{
			await _buildLock.WaitAsync(cancellationToken).ConfigureAwait(false);

			foreach (var module in modules)
			{
				var mustBeAdded = this.MustBeAdded(module, groupName);
				if (mustBeAdded)
				{
					ModuleGroup moduleGroup;
					var index = this.ModuleGroups.IndexOf(groupName);
					if (index < 0)
					{
						this.ModuleGroups.Add(moduleGroup = groupName);
					}
					else
					{
						moduleGroup = this.ModuleGroups[index];
					}

					moduleGroup.Modules.Add(module);
				}
			}
		}
		finally
		{
			_buildLock.Release();
		}
	}

	private bool MustBeAdded(IModule module, string groupName)
	{
		var alreadyExists = this.IsModuleIsAlreadyAdded(module, out var existingGroupName);
		if (!alreadyExists) return true;
		return this.TryRemoveModuleFromLaterGroup(module, groupName, existingGroupName!);
	}

#if NETFRAMEWORK || NETSTANDARD2_0
	private bool IsModuleIsAlreadyAdded(IModule module, out string? existingGroupName)
#else
		private bool IsModuleIsAlreadyAdded(IModule module, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? existingGroupName)
#endif
	{
		existingGroupName = this.ModuleGroups
				.FirstOrDefault(moduleGroup => moduleGroup.Modules.Contains(module))
				?.GroupName
			;
		return existingGroupName is not null;
	}

	private bool TryRemoveModuleFromLaterGroup(IModule module, string groupName, string existingGroupName)
	{
		var allGroupNames = this.ModuleGroups.Select(moduleGroup => moduleGroup.GroupName).ToList();
		
		var newIndex = allGroupNames.IndexOf(groupName);
		if (newIndex < 0) return false;

		var existingIndex = allGroupNames.IndexOf(existingGroupName);
		if (existingIndex <= newIndex) return false;
			
		// Remove the existing module.
		var modules = this.ModuleGroups[existingIndex].Modules;
		modules.Remove(module);

		// Remove the whole group if it is empty.
		if (!modules.Any()) this.ModuleGroups.RemoveAt(existingIndex);
			
		// Return true, so that the module will be added again.
		return true;
	}

	private static ILifetimeScope CreateChildScope(ILifetimeScope parentScope, params IModule[] modules)
	{
		return parentScope.BeginLifetimeScope
		(
			builder =>
			{
				foreach (var module in modules)
				{
					builder.RegisterModule(module);
				}
			}
		);
	}

	#endregion

	#endregion

	#region Nested Types

	internal sealed class ModuleGroup : IEquatable<ModuleGroup>
	{
		#region Delegates / Events
		#endregion

		#region Constants
		#endregion

		#region Fields
		#endregion

		#region Properties

		/// <summary> The name of the group. </summary>
		public string GroupName { get; }

		/// <summary> Collection of <see cref="IModule"/>s belonging to the group. Those compose the <see cref="ILifetimeScope"/> after build. </summary>
		public IList<IModule> Modules { get; }

		#endregion

		#region (De)Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="groupName"> <see cref="GroupName"/> </param>
		private ModuleGroup(string groupName)
		{
			this.GroupName = groupName;
			this.Modules = new List<IModule>();
		}

		public void Deconstruct(out string group, out IModule[] modules)
		{
			group = this.GroupName;
			modules = this.Modules.ToArray();
		}

		public static implicit operator ModuleGroup(string group)
		{
			return new ModuleGroup(group);
		}

		#endregion

		#region Methods

		#region IEquatable

		/// <inheritdoc />
		public bool Equals(ModuleGroup? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return this.GroupName == other.GroupName;
		}

		/// <inheritdoc />
		public override bool Equals(object? @object)
		{
			return ReferenceEquals(this, @object) || @object is ModuleGroup other && Equals(other);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return this.GroupName.GetHashCode();
		}

		public static bool operator ==(ModuleGroup? left, ModuleGroup? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ModuleGroup? left, ModuleGroup? right)
		{
			return !Equals(left, right);
		}

		#endregion

		#endregion
	}

	#endregion
}