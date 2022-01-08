# Phoenix.Functionality.Ioc

This is a collection of helper assemblies for the inversion of control principle.
___

# Table of content

[toc]
___

# Phoenix.Functionality.Ioc.Autofac

| .NET Framework | .NET Standard | .NET |
| :-: | :-: | :-: |
| :heavy_minus_sign: | :heavy_check_mark: 2.0 | :heavy_check_mark: 5.0 :heavy_check_mark: 6.0 |

This assembly is centered around the dependency injection framework [**Autofac**](https://github.com/autofac/Autofac).

## Scope Building (`NestedScopeBuilder`)

The `NestedScopeBuilder` allows to build nested **Autofac.ILifetimeScope**s. Each of those is build from a single named group consisting of multiple **Autofac.Core.IModule**s. Its main purpose is to help setting up a complex chain of nested **Autofac.ILifetimeScope**s within an applications bootstrapper or loader class.

### Usage

First an instance of a `NestedScopeBuilder` has to be created.

- Without any dependencies

	```csharp
	var builder = new NestedScopeBuilder();
	```
- With a collection of `IModules` that are used to build the initial `ILifetimeScope`

	```csharp
	class MyModule : Autofac.Module {}
	var builder = new NestedScopeBuilder(new MyModule());
	```
- With an already existing `ILifetimeScope`

	```csharp
	var initialContainer = new ContainerBuilder().Build();
	var builder = new NestedScopeBuilder(initialContainer);
	```

<div style='padding:0.1em; border-style: solid; border-width: 0px; border-left-width: 10px; border-color: #37ff00; background-color: #37ff0020' >
	<span style='margin-left:1em; text-align:left'>
    	<b>Information</b>
    </span>
    <br>
	<div style='margin-left:1em; margin-right:1em;'>
		The instance could also be created by an <b>ILifetimeScope</b> that was setup during application startup whose only purpose is to provide the most basic services like settings, the main application window and of course this <i>NestedScopeBuilder</i>, that will be responsible for setting up all other scopes that the application needs. The constructor accepting an already existing scope should be used in this case.
    </div>
</div>


After the `NestedScopeBuilder` has been created, additional scopes can be setup. During setup, each scope is identified by a custom name, that allows to combine modules together that later should compose a single **Autofac.ILifetimeScope**. In the below example the _DatabaseModule_ is added to a group name _Services_ and the other two modules are added to a group name _Application_. During build two **ILifetimeScope**s would be created, where the one from the _Application_ group would be a child scope to the _Services_ one. This guarantees, that the _Application_ scope can access all services from its parent scopes.

```csharp
class DatabaseModule : Autofac.Module {}
class MyModule : Autofac.Module {}
class YourModule : Autofac.Module {}
builder.AddModule<DatabaseModule>().ToGroup("Services");
builder.AddModules(new MyModule(), new YourModule()).ToGroup("Application");
```

<div style='padding:0.1em; border-style: solid; border-width: 0px; border-left-width: 10px; border-color: #37ff00; background-color: #37ff0020' >
	<span style='margin-left:1em; text-align:left'>
    	<b>Information</b>
    </span>
    <br>
	<div style='margin-left:1em; margin-right:1em;'>
    	The order of how the groups are created is also the order of how the different <b>ILifetimeScope</b>s are nested during build. Adding a new <b>IModule</b> to an existing group after other groups have already been created does not change the order of the group.
    </div>
</div>

<div style='padding:0.1em; border-style: solid; border-width: 0px; border-left-width: 10px; border-color: #37ff00; background-color: #37ff0020' >
	<span style='margin-left:1em; text-align:left'>
    	<b>Information</b>
    </span>
    <br>
	<div style='margin-left:1em; margin-right:1em;'>
    	When adding the same <b>IModule</b> multiple times, it will only be added, if its new group is a predecessor to its current one. This guarantees, that services of a module are always resolveable by any neseted scope.
    </div>
</div>

The final step is to build all nested scopes. The return value will be the innermost **ILifetimeScope** that has access to **all** registered services.

```cs
var finalScope = await builder.BuildAsync();
```

<div style='padding:0.1em; border-style: solid; border-width: 0px; border-left-width: 10px; border-color: #37ff00; background-color: #37ff0020' >
	<span style='margin-left:1em; text-align:left'>
    	<b>Information</b>
    </span>
    <br>
	<div style='margin-left:1em; margin-right:1em;'>
    	Every time a new <b>ILifetimeScope</b> is created by a <i>NestedScopeBuilder</i> instance, all available modules are flushed and the created scope replaces the inital one. It is therefore possible to get intermediate scopes during setup and reuse the <i>NestedScopeBuilder</i>.
    </div>
</div>



## Scope Verification

The extension method `ILifetimeScope.ExecuteVerificationMethodsAsync` can be used together with functions that have been registered as `IocScopeVerificationDelegate` to verify any given scope with custom logic. Below example shows how an **Autofac.Core.IModule** could be written, to easily provide such an verification function.

First the module registers a service that provides database connections for other services within an application. The second registration is the verification function that checks the database connection.

```csharp
class DatabaseModule : Autofac.Module
{
	protected override void Load(ContainerBuilder builder)
	{
		// Register the database service.
		builder.RegisterType<DatabaseContext>().As<IDatabaseContext>();

		// Register VerifyConnectivityAsync as verification method.
		builder.Register
			(
				context =>
				{
					var contextFactory = context.Resolve<IDatabaseContext.Factory>();
					IocScopeVerificationDelegate verificationCallback = token
						=> DatabaseModule.VerifyConnectivityAsync(contextFactory, token);
					return verificationCallback;
				}
			)
			.As<IocScopeVerificationDelegate>()
			.SingleInstance()
			;
	}

	internal static async IAsyncEnumerable<IocScopeVerificationResult> VerifyConnectivityAsync
	(
		IDatabaseContext.Factory contextFactory,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
	{
		yield return "Checking database availability";
		do
		{
			using var context = contextFactory.Invoke();
			var (isConnected, exception) = await context.CheckConnectionAsync(cancellationToken).ConfigureAwait(true);
			if (isConnected)
			{
				yield return "Connection to database has been established.";
				break;
			}
			else if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			else
			{
				yield return new IocScopeVerificationResult
				(
					"Connection to database couldn't be established. Trying again...",
					exception
				);
				try
				{
					await Task.Delay(2000, cancellationToken);
				}
				catch (OperationCanceledException) { /* ignore */ }
			}

		}
		while (!cancellationToken.IsCancellationRequested);
	}
}
```
At some point during application startup it would be a good idea to validate, if a connection to the database can be established. This is where the second registration of the `IocScopeVerificationDelegate` comes into play. The `ILifetimeScope.ExecuteVerificationMethodsAsync` extension method resolves all registered `IocScopeVerificationDelegate`s and executes them. The implemented verification functions then asynchronously return `IocScopeVerificationResult`s, that can be used for logging purposes or even be displayed as part of an application loading dialog.

```csharp
var builder = new ContainerBuilder();
builder.RegisterModule<DatabaseModule>();
var container = builder.Build();
try
{
	var asyncEnumerable = container.ExecuteVerificationMethodsAsync();
	await foreach (var result in asyncEnumerable)
	{
		/* Log the messages. */
	}
}
catch (OperationCanceledException)
{
	/* Handle cancellation. */

}
catch (IocScopeVerificationException ex)
{
	/* Handle verification exception. */
}
```
<div style='padding:0.1em; border-style: solid; border-width: 0px; border-left-width: 10px; border-color: #37ff00; background-color: #37ff0020' >
	<span style='margin-left:1em; text-align:left'>
    	<b>Information</b>
    </span>
    <br>
	<div style='margin-left:1em; margin-right:1em;'>
        Executing <i>ExecuteVerificationMethodsAsync</i> will only ever throw either an <i>OperationCanceledException</i> or an <i>IocScopeVerificationException</i>. If a verification method internally threw something else, this exception will be wrapped within the <i>IocScopeVerificationException</i> as inner exception.
    </div>
</div>

___

# Authors

* **Felix Leistner**: _v1.x_ - _v2.x_