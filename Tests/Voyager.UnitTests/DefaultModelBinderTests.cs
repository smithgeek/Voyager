using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using Voyager.Api;
using Xunit;

namespace Voyager.UnitTests
{
	public class DefaultModelBinderTests
	{
		private readonly DefaultModelBinder binder;
		private readonly HttpContext httpContext;
		private readonly IServiceProvider provider;

		public DefaultModelBinderTests()
		{
			var services = new ServiceCollection();
			services.AddVoyager(c => c.AddAssemblyWith<BlankPolicy>());
			provider = services.BuildServiceProvider();
			binder = provider.GetService<DefaultModelBinder>();
			httpContext = new DefaultHttpContext
			{
				RequestServices = services.BuildServiceProvider(),
			};
		}

		[Fact]
		public async Task Test()
		{
			using var stream = new MemoryStream();
			using var writer = new StreamWriter(stream);
			writer.Write("{\"SomeNumber\": 33, \"SomeString\": \"value\", \"complex\": { \"boolean\": true, \"string\": \"more data\"}}");
			writer.Flush();
			stream.Position = 0;
			httpContext.Request.Body = stream;
			httpContext.Request.QueryString = new QueryString("?value=7");
			httpContext.Request.ContentType = "application/json";
			var request = await binder.Bind<Request>(httpContext);
			request.Complex.Boolean.Should().BeTrue();
			request.Complex.String.Should().Be("more data");
			request.SomeNumber.Should().Be(33);
			request.SomeString.Should().Be("value");
			request.QueryValue.Should().Be(7);
			request.NotExist.Should().BeNull();
		}

		public class ComplexObject
		{
			public bool Boolean { get; set; }
			public string String { get; set; }
		}

		public class Request
		{
			public ComplexObject Complex { get; set; }
			public string NotExist { get; set; }

			[FromQuery("value")]
			public int QueryValue { get; set; }

			public int SomeNumber { get; set; }
			public string SomeString { get; set; }
		}
	}
}