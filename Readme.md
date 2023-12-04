# Voyager

A source generator to make request/endpoint/response even easier.

Easily create decoupled components for every route endpoint.

[![Build Status](https://dev.azure.com/smithgeek/Voyager/_apis/build/status/smithgeek.Voyager?branchName=master)](https://dev.azure.com/smithgeek/Voyager/_build/latest?definitionId=14&branchName=master)

## Contents

[Install](#Install)

[Getting Started](#Getting-Started)

[Endpoints](#Endpoints)

[Model Binding](#Model-Binding)

[Validation](#Validation)

[Configuration](#Configuration)

[OpenApi](#Open-Api)

# Install

You should install using nuget

```
Install-Package Voyager
```

or

```
dotnet add package Voyager
```

# Getting Started

A minimal voyager application setup is as follows.

```cs
using Voyager;

var builder = WebApplication.CreateBuilder(args);
builder.services.AddVoyager();

var app = builder.Build();
app.MapVoyager();
app.Run();
```

You need to add a `using Voyager;` to your file and then add `builder.services.AddVoyager();` and `app.MapVoyager();`

# Endpoints

Endpoints in Voyager are classes with a `[VoyagerEndpoint]` attribute. You specify the path as an argument to the attribute.

```cs
[VoyagerEndpoint("weatherForecast/{city}")]
public class WeatherForecastEndpoint
{
	public WeatherForecast Get(WeatherForecastRequest request)
	{
		// ...request logic
	}
}
```

Add Get, Post, Put, etc methods to handle those http methods.

Requests and responses are just plain c# objects.

```cs
public class WeatherForecastRequest
{
    [FromRoute]
	public required string City { get; set; }

	[FromQuery]
	public int Days { get; set; } = 5;
}

public record WeatherForecast(string City, DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

# Model Binding

By default Voyager assumes the request body is in json format and will deserialize the body into your request object.

You can use the `[FromForm]`, `[FromRoute]`, `[FromQuery]`, `[FromHeader]`, and `[FromCookie]` attributes to get the data from somewhere else.

# Validation

Validation is performed using [Fluent Validation](https://fluentvalidation.net/). Add a static function named `AddValidationRules` to your request object to add validation rules.

```cs
public class WeatherForecastRequest
{
    [FromRoute]
	public required string City { get; set; }

	[FromQuery]
	public int Days { get; set; } = 5;

	public static void AddValidationRules(AbstractValidator<WeatherForecastRequest> validator)
	{
		validator.RuleFor(r => r.Days).InclusiveBetween(1, 5);
	}
}
```

# Configuration

Voyager generates code that uses Minimal Api's. That means that you can add any configuration that minimal api support if needed. All you have to do is add a static Configure function to your endpoint.

```cs
[VoyagerEndpoint("weatherForecast/{city}")]
public class WeatherForecastEndpoint
{
	public WeatherForecast Get(WeatherForecastRequest request)
	{
		// ...request logic
	}

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder.CacheOutput();
	}
}
```

# Open Api

If you want to use OpenApi you should add voyager to the swagger generation so that all the schema types are added to the document.

```cs
builder.Services.AddSwaggerGen(config => config.AddVoyager());
```
