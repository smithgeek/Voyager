# Voyager

An alternative routing system utilizing [Mediatr](https://github.com/jbogard/MediatR).

Easily create decoupled components for every route endpoint.

[![Build Status](https://dev.azure.com/smithgeek/Voyager/_apis/build/status/smithgeek.Voyager?branchName=master)](https://dev.azure.com/smithgeek/Voyager/_build/latest?definitionId=14&branchName=master)

Read the [announcement blog post](https://smithgeek.com/announcing-voyager/).

## Contents

[Install](#Install)

[Getting Started](#Getting-Started)

[Azure Functions Setup](#Azure-Functions-Setup)

[Requests and Handlers](#Requests-and-Handlers)

[Model Binding](#Model-Binding)

[Validation](#Validation)

[Authorization](#Authorization)

[Azure Functions Forwarder](#Azure-Functions-Forwarder)

## Install

You should install using nuget

```
Install-Package Voyager
```

or

```
dotnet add package Voyager
```

## Azure Functions Install

If you're running in Azure Functions you'll also want to install

```
Install-Package Voyager.Azure.Functions
```

or

```
dotnet add package Voyager.Azure.Functions
```

## Getting Started

To add Voyager to your application you need to add the following in ConfigureServices. You need to let Voyager know of all the assemblies that will have requests or handlers.

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddVoyager(c =>
    {
        c.AddAssemblyWith<Startup>();
    });
}
```

You will also need to add `endpoints.MapVoyager();` in your UseEndpoints configuration. Voyager also provides an exception handler middleware. However, this middleware is not required to use Voyager. An example of a configure method is

```cs
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    app.UseVoyagerExceptionHandler();
    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();
    app.UseMiddleware<SampleMiddleware>();
    app.UseEndpoints(endpoints =>
    {
	endpoints.MapVoyager();
        endpoints.MapControllers();
    });
}
```

## Azure Functions Setup

Create a startup class that looks similar to the one below. The goal was to provide a nearly identitical startup process that you use in a normal aspnet core api application. You are also free to add other middleware during configuration.

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
        app.UseRouting();
        app.UseMiddleware<SampleMiddleware>();
        app.UseEndpoints(endpoints =>
	{
		endpoints.MapVoyager();
	})
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
        });
    }
}
```

## Requests and Handlers

Requests in Voyager are modeled as a class with a `[VoyagerRoute]` Attribute. You must specify the http method and the route path this request is modeling.

```cs
[VoyagerRoute(HttpMethod.Get, "voyager/info")]
public class GetVoyagerInfoRequest : EndpointRequest
{
}
```

To handle this request you then need to write a handler class.

```cs
public class GetVoyagerInfoHandler : EndpointHandler<GetVoyagerInfoRequest>
{
    public override IActionResult HandleRequest(GetVoyagerInfoRequest request)
    {
        return Ok(new { Message = "Voyager is awesome!" });
    }
}
```

The EndpointHandler provides some convenience methods that you might be use to from controller classes (like Ok(), BadRequest(), etc.). If you prefer not to inherit from the base class you can also just implement the `IEndpointHandler` interface.

If you want to have a strongly typed return value you can do that too!

```cs
public class GetVoyagerInfoResponse
{
    public string Message { get; set; }
}

[Route(HttpMethod.Get, "voyager/info")]
public class GetVoyagerInfoRequest : EndpointRequest<GetVoyagerInfoResponse>
{
}

public class GetVoyagerInfoHandler : EndpointHandler<GetVoyagerInfoRequest, GetVoyagerInfoResponse>
{
    public override ActionResult<GetVoyagerInfoResponse> HandleRequest(GetVoyagerInfoRequest request)
    {
        return new GetVoyagerInfoResponse{ Message = "Voyager is awesome!" };
    }
}
```

# Model Binding

By default Voyager assumes the request body is in json format and will deserialize the body into your request object.

You can use the `[FromForm]`, `[FromRoute]`, and `[FromQuery]` attributes to get the data from somewhere else.

```cs
// Request: /example/delete?n=3
[Route(HttpMethod.Post, "example/{action}")]
public class ExampleRequest
{
    public string SomeDataFromJsonBody { get; set; }

    [FromRoute]
    public string Action { get; set; }

    [FromQuery("n")]
    public int Number { get; set; }
}
```

## Validation

Validation is performed using [Fluent Validation](https://fluentvalidation.net/). Simply add a Validator class for your Request object.

```cs
public class ExampleRequestValidator : AbstractValidator<ExampleRequest>
{
    public ExampleRequestValidator()
    {
        RuleFor(r => r.Number).GreaterThanOrEqualTo(1);
    }
}
```

## Authorization

You can use the same authorization attributes that you use with MVC.

```cs
[Authorize(Policy = "MyPolicy")]
public class GetVoyagerInfoRequest : EndpointRequest
{
    ...
}
```

Voyager also provides an alternative way to create and apply policies while still using standard ASP.NET requirements and handlers. It provides type safety instead of relying on strings.

You can make your own policies by creating a class that implements the [Policy](src/Voyager/Api/Authorization/Policy.cs) interface. The interface requires a single GetRequirements function that returns a list of all the requirements that must be satisfied. Returning an empty list is allowed.

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

Voyager provides an [AnonymousPolicy](src/Voyager/Api/Authorization/AnonymousPolicy.cs) and [AuthenticatedPolicy](src/Voyager/Api/Authorization/AuthenticatedPolicy.cs) for you. If you don't specify a policy, anyone can access the endpoint.

You apply a policy by adding the [Enforce](src/Voyager/Api/Authorization/Enforce.cs) interface and providing a policy.

```cs
public class GetVoyagerInfoRequest : EndpointRequest, Enforce<AuthenticatedPolicy>
{
    ...
}
```

## Exceptions

The `UseVoyagerExceptionHandler` middleware will catch any exceptions thrown. In development mode the exception is returned in a 500 response. In other modes, an empty 500 body is returned.

## Azure Functions Forwarder

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
    public Task<IActionResult> FallbackRoute([HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "delete", "post", "head", "trace", "patch", "connect", "options", Route = "{*path}")] HttpRequest req)
    {
        return router.Route(req.HttpContext);
    }
}
```
