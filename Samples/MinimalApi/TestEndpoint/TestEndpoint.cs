using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Validot;
using Voyager;

namespace Shared.TestEndpoint;

public class EvaluateResponse
{
	public bool Success { get; set; } = false;
	public string Message { get; set; } = string.Empty;
}

[VoyagerEndpoint("/propertyInjection")]
public class TestEndpointHandler
{
	public required HttpContext HttpContext { get; set; }

	public EvaluateResponse Get()
	{
		return new EvaluateResponse
		{
			Success = HttpContext != null,
		};
	}
}

public class FluentValidationRequest
{
	public required string Text { get; set; }
}

public class FluentValidationRequestValidator : AbstractValidator<FluentValidationRequest>
{
	public FluentValidationRequestValidator()
	{
		//RuleFor(r => r.Text).NotNull();
	}
}

[VoyagerEndpoint("/validation/fluent")]
public class FluentValidationHandler
{
	public EvaluateResponse Post(FluentValidationRequest request)
	{
		return new() { Success = true };
	}
}

public class ValidotRequest
{
	public string? Text { get; set; }
}

public class ValidotRequestSpecificationHolder : ISpecificationHolder<ValidotRequest>
{
	public ValidotRequestSpecificationHolder()
	{
		Specification = s => s.Member(m => m.Text, m => m.Required());
	}

	public Specification<ValidotRequest> Specification { get; }
}

[VoyagerEndpoint("/validation/validot")]
public class ValidotHandler
{
	public EvaluateResponse Post(ValidotRequest request)
	{
		return new() { Success = true };
	}
}

public class DefaultValidtorRequest
{
	public required string Text { get; set; }
}

[VoyagerEndpoint("/validation/default")]
public class DefaultValidationHandler
{
	public EvaluateResponse Post(DefaultValidtorRequest request)
	{
		return new() { Success = true };
	}
}

[VoyagerEndpoint("/validation/handledByHandler")]
public class ValidationHandledByHandler
{
	public EvaluateResponse Post(DefaultValidtorRequest request, Voyager.Validation.ValidationSummary validationSummary)
	{
		return new() { Success = validationSummary.IsValid, Message = "Handler" };
	}
}

[VoyagerEndpoint("/configured")]
public class ConfigureEndpoint
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder.RequireHost("smithgeek.com");
	}

	public EvaluateResponse Get()
	{
		return new() { Success = true };
	}
}

[VoyagerEndpoint("/cancelToken")]
public class CancellationTokenEndpoint
{
	public EvaluateResponse Get(CancellationToken cancellationToken)
	{
		return new() { Success = cancellationToken != CancellationToken.None };
	}
}

// anonymous response

[VoyagerEndpoint("/anonymousResponse")]
public class AnonymousResponse
{
	public IResult Post(Body request)
	{
		if (request.Test == null)
		{
			var response = new ObjectResponse
			{
				Text = "1"
			};
			return TypedResults.Ok(response);
		}
		var response2 = new ObjectResponse
		{
			Text = "2"
		};
		return TypedResults.Ok(response2);
	}

	public class Body
	{
		public string? Test { get; init; }
	}

	public class ObjectResponse
	{
		public required string Text { get; init; }
		public string? OtherText { get; init; }
	}
}

[VoyagerEndpoint("/multipleInjections")]
public class MultipleInjections
{
	public IResult Get(Service service)
	{
		return TypedResults.Ok();
	}

	public IResult Post(Service service)
	{
		return TypedResults.Ok();
	}
}

[VoyagerEndpoint("/records")]
public class RecordsEndpoint
{
	public record RecordRequest([FromQuery] string Id, int Value, string? Text, string Name, Policy policy);

	public static IResult Post(RecordRequest request)
	{
		return TypedResults.Ok(new { value = $"{request.Id} {request.Value} {request.Name} {request.policy.Rules.Count} {request.policy.Rules[0].Value}" });
	}
}

public class Policy
{
	public required List<Rule> Rules { get; init; }
}

public class Rule
{
	public int Value { get; init; }
}