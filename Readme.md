# Setup
To add Voyager to your application you need to add the following in ConfigureServices. You will want to add all of your application assemblies that have requests and handlers.

```cs
services.AddVoyager(configure => {
	configure.AddAssemblyWith<Startup>();
});
```

You will also want to add `UseVoyagerRouting` and `UseVoyagerEndpoints` to your `Configure` method. It is recommended to add `UseVoyagerExceptionHandler` as well. An example of a configure method is
```cs
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
	if (env.IsDevelopment())
	{
		app.UseDeveloperExceptionPage();
	}
	app.UseVoyagerExceptionHandler();
	app.UseHttpsRedirection();

	app.UseVoyagerRouting();
	app.UseRouting();

	app.UseAuthorization();
	app.UseMiddleware<SampleMiddleware>();
	app.UseEndpoints(endpoints =>
	{
		endpoints.MapControllers();
	});
	app.UseVoyagerEndpoints();
}
```

If you are only using Voyager routing you can remove `UseRouting` and `UseEndpoints`. 


# Azure Functions Setup
If you want to run on Azure functions you need to have the `Voyager.Azure.Functions` package installed. Then create a startup class that looks similar to the one below. The goal was to provide a nearly identitical startup process that you use in a normal aspnet api application. You are also free to add other middleware during configuration.

```cs
public class Startup : FunctionsStartup
{
	public Startup()
	{
	}

	public void Configure(IApplicationBuilder app)
	{
		app.UsePathBase("/api");
		app.UseVoyagerExceptionHandler();
		app.UseVoyagerRouting();
		app.UseMiddleware<SampleMiddleware>();
		app.UseVoyagerEndpoints();
	}

	public override void Configure(IFunctionsHostBuilder builder)
	{
		builder.AddVoyager(ConfigureServices, Configure);
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddVoyager(c =>
		{
			c.AddAssemblyWith<Startup>();
			c.AddAssemblyWith<SampleMiddleware>();
		});
	}
}
```

# Routing
Routing is determined by adding the `[Route]` attribute to your Request class.

If you are running in Azure Functions you should add something like the following function to forward all requests to Voyager.

```cs
public class Routes
{
	private readonly HttpRouter router;

	public Routes(HttpRouter router)
	{
		this.router = router;
	}

	[FunctionName(nameof(FallbackRoute))]
	public Task<IActionResult> FallbackRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", "post", "head", "trace", "patch", "connect", "options", Route = "{*rest}")] HttpRequest req)
	{
		return router.Route(req.HttpContext);
	}
}
```

Routing uses [MediatR](https://github.com/jbogard/MediatR) so whatever Request type you use should have a handler.

### Handlers
Requests are routed to any mediator handler, however this library provides a convenience base class called [EndpointHandler](Voyager/Api/EndpointHandler.cs). 

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
			return Ok();
		}
		return Unauthorized();
	}
}
```

If your handler is returning data it is recommended to use the 3 template version of EndpointHandler. This will provide strong typing everywhere, make testing easier, and allow response types in [OpenApi](https://www.openapis.org/)

```cs
public class LoginRequest : EndpointRequest<LoginResponse>
{
}

public class LoginResponse
{
	public bool Success { get; set; }
}

public class LoginRequestHandler : EndpointHandler<LoginRequest, LoginResponse, AnonymousPolicy>
{
	public LoginRequestHandler(EndpointServices services)
		: base(services)
	{
	}

	public override async Task<LoginResponse> HandleRequestAsync(LoginRequest request)
	{
		if(/* do authentication checking*/)
		{
			return new LoginResponse{ Success = true };
		}
		return new LoginResponse{ Success = false };
	}
}
```

### Validation
Validation is performed using [Fluent Validation](https://fluentvalidation.net/). Simply add a Validator class for your Request object.

### Policies
Policy classes must implement the [Policy](Voyager/Api/Authorization/Policy.cs) interface which requires a single GetRequirements function that returns a list of all the requirements that must be satisfied. Returning an empty list is allowed.
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
The UseVoyagerExceptionHandler middleware will catch any exceptions thrown. In development mode the exception is returned in a 500 response. In other modes, an empty 500 body is returned.

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
Almost all behaviors are behind an interface. If you want to replace a certain piece of functionality you can create your own implementation. Any interfaces registered before AddVoyager is called will replace the implementation in the library.
