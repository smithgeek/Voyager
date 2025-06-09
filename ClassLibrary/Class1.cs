using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Voyager;
using Voyager.Validation;

namespace ClassLibrary;

public class Request
{
	public string Name { get; set; } = string.Empty;
}

[VoyagerEndpoint("/test")]
public class Handler
{
	public bool Post(Request request)
	{
		return true;
	}
}

public class Request2
{
	public required string Name { get; set; }

	[FromRoute]
	public int Index { get; set; }
}

public class Request2Validator : AbstractValidator<Request2>
{
	public Request2Validator()
	{
		RuleFor(r => r.Name).NotNull();
		RuleFor(r => r.Index).GreaterThanOrEqualTo(0);
	}
}

[VoyagerEndpoint("/test/{index}")]
public class Handler2
{
	public bool Post(Request2 request, ValidationSummary validationResult)
	{
		return true;
	}
}

public class EmptyRequest
{

}

[VoyagerEndpoint("/test/noreq")]
public class EmptyRequestHandler
{
	public bool Get(EmptyRequest request)
	{
		return true;
	}
}

public static class Startup
{
	public static void Run(IServiceCollection services)
	{
		services.AddVoyager(c =>
		{
			c.AddFluentValidation();
		});
	}
}

public enum EventType
{
	INSERT,
	UPDATE,
	DELETE
}

public class SupabasePayload<T>
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[JsonPropertyName("type")]
	public required EventType Type { get; init; }

	[JsonPropertyName("table")]
	public required string Table { get; init; }

	[JsonPropertyName("schema")]
	public required string Schema { get; init; }

	[JsonPropertyName("record")]
	public T? Record { get; init; }

	[JsonPropertyName("old_record")]
	public T? OldRecord { get; init; }
}

public class Display
{
	[JsonPropertyName("id")]
	public required Guid Id { get; init; }
}

[VoyagerEndpoint("/generic/req")]
public class GenericRequestHandler
{
	public bool Post(SupabasePayload<Display> request)
	{
		return true;
	}
}