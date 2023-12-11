using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Voyager;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Services
	.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddVoyager();
builder.Services.AddVoyager2();

var app = builder.Build();
app.UseAuthorization();
app.MapVoyager();
app.Urls.Add("http://0.0.0.0:5000");
app.Run();

namespace VoyagerApi
{
	public partial class Program { }

	public partial class Request
	{
		[FromRoute(Name = "id")]
		public int Id { get; init; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public int Age { get; set; }
		public IEnumerable<string>? PhoneNumbers { get; set; }

		public static void Validate(AbstractValidator<Request> validator)
		{
			validator.RuleFor(x => x.FirstName).NotEmpty().WithMessage("name needed");
			validator.RuleFor(x => x.LastName).NotEmpty().WithMessage("last needed");
			validator.RuleFor(x => x.Age).GreaterThan(10).WithMessage("too young");
			validator.RuleFor(x => x.PhoneNumbers).NotEmpty().WithMessage("phone needed");
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
				Id = req.Id,
				Name = req.FirstName + " " + req.LastName,
				Age = req.Age,
				PhoneNumber = req.PhoneNumbers?.FirstOrDefault()
			};
		}
	}

	public class Response
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public int Age { get; set; }
		public string? PhoneNumber { get; set; }
	}
}