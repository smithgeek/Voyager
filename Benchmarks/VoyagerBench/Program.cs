using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Validot;
using Voyager;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Services
	.AddAuthorization();
builder.Services.AddVoyager();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
	config.AddVoyager();
	config.SupportNonNullableReferenceTypes();
});

var app = builder.Build();
app.UseAuthorization();
app.MapVoyager();

app.MapSwagger();
app.UseSwagger();

app.Urls.Add("http://0.0.0.0:5000");
app.Run();

namespace VoyagerApi
{
	public partial class Program { }

	public record Request(string? FirstName)
	{
		[FromRoute(Name = "id")]
		public int UserId { get; init; }
		public string? LastName { get; set; }
		public int Age { get; set; }
		public IEnumerable<string>? PhoneNumbers { get; set; }

		public static void Validate(AbstractValidator<Request> validator)
		{
			validator.RuleFor(x => x.FirstName).NotEmpty().WithMessage("name needed");
			validator.RuleFor(x => x.LastName).NotEmpty().WithMessage("last needed");
			validator.RuleFor(x => x.Age).GreaterThan(10).WithMessage("too young");
			validator.RuleFor(x => x.PhoneNumbers).NotEmpty().WithMessage("phone needed");
			validator.RuleFor(x => x.UserId).GreaterThan(5).WithMessage("id must be greater than 5");
		}
	}

	public record ValidotRequest(string? FirstName)
	{
		[FromRoute(Name = "id")]
		public int UserId { get; init; }
		public string? LastName { get; set; }
		public int Age { get; set; }
		public IEnumerable<string>? PhoneNumbers { get; set; }

		public static Validot.IValidator<ValidotRequest> CreateValidator()
		{
			Specification<ValidotRequest> spec = _ => _
				.Member(m => m.FirstName, m => m.NotEmpty().WithMessage("name needed"))
				.Member(m => m.LastName, m => m.NotEmpty().WithMessage("last needed"))
				.Member(m => m.Age, m => m.GreaterThan(10).WithMessage("too young"))
				.Member(m => m.PhoneNumbers, m => m.NotEmptyCollection().WithMessage("phone needed"))
				.Member(m => m.UserId, m => m.GreaterThan(5).WithMessage("id must be greater than 5"));
			return Validator.Factory.Create(spec);
		}
	}

	interface IConfigurableEndpoint
	{
		static abstract void Configure(RouteHandlerBuilder routeHandlerBuilder);
	}

	[VoyagerEndpoint("/norequest")]
	public class NoRequestEndpoint
	{
		public int Get()
		{
			return 32;
		}
	}

	[VoyagerEndpoint("/static")]
	public class StaticEndpoint
	{
		public static IResult Get(Service service)
		{
			return TypedResults.Ok(new { test = true });
		}
	}

	[VoyagerEndpoint("/benchmark/ok/{id}")]
	public class Endpoint : IConfigurableEndpoint
	{
		public static void Configure(RouteHandlerBuilder routeHandlerBuilder)
		{
			routeHandlerBuilder
				.RequireAuthorization()
				.AllowAnonymous();
		}

		public Response Post(Request req, ILogger<Program> logger)
		{
			return new Response()
			{
				Id = req.UserId,
				Name = req.FirstName + " " + req.LastName,
				Age = req.Age,
				PhoneNumber = req.PhoneNumbers?.FirstOrDefault()
			};
		}
	}

	[VoyagerEndpoint("/validot/benchmark/ok/{id}")]
	public class ValidotEndpoint : IConfigurableEndpoint
	{
		public static void Configure(RouteHandlerBuilder routeHandlerBuilder)
		{
			routeHandlerBuilder
				.RequireAuthorization()
				.AllowAnonymous();
		}

		public Response Post(ValidotRequest req, ILogger<Program> logger, Validot.Results.IValidationResult validationResult)
		{
			return new Response()
			{
				Id = req.UserId,
				Name = req.FirstName + " " + req.LastName,
				Age = req.Age,
				PhoneNumber = req.PhoneNumbers?.FirstOrDefault()
			};
		}
	}

	[VoyagerEndpoint("/anonymous")]
	public class AnonymousEndpoint
	{
		public IResult Get(Body request)
		{
			if (request.Test != null)
			{
				return TypedResults.Ok(new
				{
					something = "here"
				});
			}
			return TypedResults.Ok(new
			{
				result = request.Test
			});
		}

		public class Body
		{
			public string? Test { get; init; }
		}
	}

	namespace Duplicate
	{
		[VoyagerEndpoint("/duplicate/anonymous")]
		public class AnonymousEndpoint
		{
			public IResult Get(Body request)
			{
				if (request.Test != null)
				{
					return TypedResults.Ok(new
					{
						something = "here"
					});
				}
				return TypedResults.Ok(new
				{
					result = request.Test
				});
			}

			public class Body
			{
				public string? Test { get; init; }
				public int Value { get; init; }
			}
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

	public class Response
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public int Age { get; set; }
		public string? PhoneNumber { get; set; }
	}

	public class Service
	{

	}

	[VoyagerEndpoint("/records")]
	public class RecordsEndpoint
	{
		public record Request([FromQuery] string Id, int Value, string? Text, string Name, [FromQuery] int days = 5)
		{
			public string? Other { get; init; }

			public static void Validate(AbstractValidator<Request> validator)
			{
				validator.RuleFor(r => r.Id).NotEmpty();
			}
		}

		public static Response Get(Request request)
		{
			return new Response($"{request.Id} {request.Value} {request.Name}");
		}

		public static bool Delete(Request request)
		{
			return true;
		}

		public record Response(string Value);
	}
}