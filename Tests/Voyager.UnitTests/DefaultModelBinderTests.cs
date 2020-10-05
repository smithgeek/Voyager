using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
			var data = new Request
			{
				Complex = new ComplexObject { Boolean = true, String = "more data" },
				Int = 33,
				String = "value",
				Bool = true,
				Byte = 4,
				Char = 'z',
				DateTime = DateTime.Parse("2020-09-28T14:18:17Z"),
				DateTimeOffset = DateTimeOffset.Parse("2020-09-28T14:18:17Z"),
				Decimal = 32.3m,
				Double = 22.1d,
				Long = 1241241,
				SByte = 2,
				Short = 30,
				Float = 12.3f,
				TimeSpan = TimeSpan.FromSeconds(80),
				UShort = 2,
				UInt = 100,
				ULong = 700,
				Uri = new Uri("https://github.com/smithgeek/voyager"),
				Version = new Version("1.2.3.4"),
				Guid = Guid.NewGuid()
			};
			var json = System.Text.Json.JsonSerializer.Serialize(data);
			writer.Write(json);
			writer.Flush();
			stream.Position = 0;
			httpContext.Request.Body = stream;
			httpContext.Request.QueryString = new QueryString("?value=7");
			httpContext.Request.ContentType = "application/json";
			var request = await binder.Bind<Request>(httpContext);
			request.Complex.Boolean.Should().Be(data.Complex.Boolean);
			request.Complex.String.Should().Be(data.Complex.String);
			request.Int.Should().Be(data.Int);
			request.String.Should().Be(data.String);
			request.QueryValue.Should().Be(7);
			request.NotExist.Should().BeNull();
			request.Bool.Should().Be(data.Bool);
			request.Byte.Should().Be(data.Byte);
			request.Char.Should().Be(data.Char);
			request.DateTime.Should().Be(data.DateTime);
			request.DateTimeOffset.Should().Be(data.DateTimeOffset);
			request.Decimal.Should().Be(data.Decimal);
			request.Double.Should().Be(data.Double);
			request.Long.Should().Be(data.Long);
			request.SByte.Should().Be(data.SByte);
			request.Short.Should().Be(data.Short);
			request.Float.Should().Be(data.Float);
			request.TimeSpan.Should().Be(data.TimeSpan);
			request.UShort.Should().Be(data.UShort);
			request.UInt.Should().Be(data.UInt);
			request.ULong.Should().Be(data.ULong);
			request.Uri.Should().Be(data.Uri);
			request.Version.Should().Be(data.Version);
		}

		public class ComplexObject
		{
			public bool Boolean { get; set; }
			public string String { get; set; }
		}

		public class CustomTimeSpanConverter : JsonConverter<TimeSpan>
		{
			public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				throw new NotImplementedException();
			}

			public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value.ToString());
			}
		}

		public class CustomVersionConverter : JsonConverter<Version>
		{
			public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				throw new NotImplementedException();
			}

			public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value.ToString());
			}
		}

		public class Request
		{
			public bool Bool { get; set; }
			public byte Byte { get; set; }
			public char Char { get; set; }
			public ComplexObject Complex { get; set; }
			public DateTime DateTime { get; set; }
			public DateTimeOffset DateTimeOffset { get; set; }
			public decimal Decimal { get; set; }
			public double Double { get; set; }
			public float Float { get; set; }
			public Guid Guid { get; set; }
			public int Int { get; set; }
			public long Long { get; set; }
			public string NotExist { get; set; }

			[FromQuery("value")]
			public int QueryValue { get; set; }

			public sbyte SByte { get; set; }
			public short Short { get; set; }
			public string String { get; set; }

			[System.Text.Json.Serialization.JsonConverter(typeof(CustomTimeSpanConverter))]
			public TimeSpan TimeSpan { get; set; }

			public uint UInt { get; set; }
			public ulong ULong { get; set; }
			public Uri Uri { get; set; }
			public ushort UShort { get; set; }

			[System.Text.Json.Serialization.JsonConverter(typeof(CustomVersionConverter))]
			public Version Version { get; set; }
		}
	}
}