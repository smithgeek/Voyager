# Setup
To add the quick start system to your application you need to add the following in ConfigureServices. You will want to add all of your application assemblies that have requests, handlers, or overrides for any quick start services.

```cs
services.AddApiQuickStart().AddAssemblyWith<Startup>().Configure(config => 
{
	{configure your setup here}
});
```

Each package will provide it's own extension methods on the configure object so you can choose what packages are built.

# Configuration
All configuration options used by the quick start system start with the ApiQuickStart name. Each package can then place their own configuration options under that namespace.

Using appsettings.json
```json
{
	"ApiQuickStart": {
		"PackageName": {
			"Key": "Value"
		}
	}
}
```

Using local.settings.json
```json
{
	"Values": {
		"ApiQuickStart:PackageName:Key": "Value"
	}
}
```

You can also set values using one of the following environment variable naming conventions.

|Environment Variable Name|
|------------------------|
|ApiQuickStart:PackageName:Key|
|ApiQuickStart__PackageName__Key|

## Core Configuration Options
#### EnvironmentName
You can set the environment name by setting `ApiQuickStart:EnvironmentName`. If you don't set a value it will look for the `ASPNETCORE_ENVIRONMENT` value and then default to default to `DEVELOPMENT` if nothing is found.

# Routing
Routing is determined by adding the `[Route]` attribute to your Request class.

If you are running normal ASP.NET Core make sure you add the following to your Configure function.
```cs
app.UseApiQuickStart();
```	

If you are running in Azure Functions you should instead add something like the following function to forward all requests.

```cs
public class Routes
{
	private readonly HttpRouter router;

	public Routes(HttpRouter router)
	{
		this.router = router;
	}

	[FunctionName(nameof(FallbackRoute))]
	public Task<object> FallbackRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", "post", "head", "trace", "patch", "connect", "options", Route = "{*rest}")] HttpRequest req)
	{
		return router.Route(req.HttpContext);
	}
}
```

Routing uses MediatR so whatever Request type you use should have a handler that will return an IActionResult.

If you provide an implementation of the [EndpointExceptionAction](Smithgeek.ApiQuickStart.Core/Mediatr/EndpointExceptionAction.cs) interface you will have the opportunity to perform custom actions when exceptions are caught.

### Handlers
Requests are routed to any mediator handler, however this library provides a convenience base class called [EndpointHandler](Smithgeek.ApiQuickStart.Core/Api/EndpointHandler.cs). 

It takes two generic parameters. The first is the request object that you used in the Route method above. The second is a policy class (see more below).

The base class handles checking with IAuthorizationService (using standard .NET core [Requirements](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-3.1#requirements) and [Handlers](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-3.1#authorization-handlers)) to see if the request is allowed.
```cs
public class LoginRequestHandler : EndpointHandler<LoginRequest, AnonymousPolicy>
{
	public LoginRequestHandler(EndpointServices services)
		: base(services)
	{
	}

	public override async Task<IActionResult> HandleRequestAsync(LoginRequest request)
	{
		if(/* do authentication checking*/)
		{
			return Ok(/*some data*/);
		}
		return Unauthorized();
	}
}
```

### Policies
Policy classes must implement the [Policy](Smithgeek.ApiQuickStart.Core/Api/Authorization/Policy.cs) interface which requires a single GetRequirements function that returns a list of all the requirements that must be satisfied. Returning an empty list is allowed.
```cs
public class AuthenticatedPolicy : Policy
{
	public IList<IAuthorizationRequirement> GetRequirements()
	{
		return new IAuthorizationRequirement[]
		{
			new AuthenticatedRequirement(),
		};
	}
}
```

### Exceptions
The mediator pipeline has a behavior to catch exceptions and do something with them. In development mode the exception is returned in a 500 response. In other modes, an empty 500 body is returned.

# Model Binding
The default model binding behavior is to assume that a json body should be deserialized into the request object. However you can use the standard model binding attributes to change the behavior. You can provide a new name if the property name doesn't match what you get in the request.
```cs
[Route(HttpMethod.Post, "user/login")]
public class LoginRequest
{
	[FromForm("email")]
	public string UserId { get; set; }

	[FromForm]
	public string Password { get; set; }
}
```
You can use the following attributes in your request class.
```cs
[FromForm("somename")]
[FromRoute]
[FromQuery]
```

# Overriding Behavior
Almost all behaviors are behind an interface. If you want to replace a certain piece of functionality you can create your own implementation. Any interfaces registered before AddApiQuickStart is called will replace the implementation in the library.

# Other Packages
There are multiple packages that build on top of the core of ApiQuickStart you can pick and choose what you want to use in your application.
* [Auth](Smithgeek.ApiQuickStart.Auth/Readme.md)
* [Auth.Azure.TableStorage](Smithgeek.ApiQuickStart.Auth.Azure.TableStorage/Readme.md)
* [Auth.SqlServer](Smithgeek.ApiQuickStart.Azure.Auth.SqlServer/Readme.md)
* [Azure.BlobStorage](Smithgeek.ApiQuickStart.Azure.BlobStorage/Readme.md)
* [Azure.TableStorage](Smithgeek.ApiQuickStart.Azure.TableStorage/Readme.md)
* [SendGrid](Smithgeek.ApiQuickStart.SendGrid/Readme.md)
* [Sentry](Smithgeek.ApiQuickStart.Sentry/Readme.md)
* [Stripe](Smithgeek.ApiQuickStart.Stripe/Readme.md)