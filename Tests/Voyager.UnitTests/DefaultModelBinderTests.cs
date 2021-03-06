﻿using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Voyager.Api;
using Voyager.UnitTests.Models;
using Xunit;

namespace Voyager.UnitTests
{
	public class DefaultModelBinderTests : IDisposable
	{
		private readonly DefaultModelBinder binder;
		private readonly MemoryStream bodyStream = new MemoryStream();
		private readonly HttpContext httpContext;
		private readonly IServiceProvider provider;

		public DefaultModelBinderTests()
		{
			var services = new ServiceCollection();
			services.ConfigureOptions<JsonConfigureOptions>();
			services.AddVoyager(c => c.AddAssemblyWith<BlankPolicy>());
			provider = services.BuildServiceProvider();
			binder = provider.GetService<DefaultModelBinder>();
			httpContext = new DefaultHttpContext
			{
				RequestServices = services.BuildServiceProvider(),
			};
		}

		public void Dispose()
		{
			bodyStream?.Dispose();
		}

		[Fact]
		public async Task DontParseBodyIfValuesAreElsewhere()
		{
			await BindRequest<User>("{\"nothing\": \"important\"}", new RequestOptions
			{
				QueryString = "?userId=user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72"
			});
			httpContext.Request.Body.Position.Should().Be(0);
		}

		[Fact]
		public async Task DontParseIfNoProperties()
		{
			await BindRequest<EmptyRequest>("{\"nothing\": \"important\"}");
			httpContext.Request.Body.Position.Should().Be(0);
		}

		[Fact]
		public async Task IdInQuery()
		{
			var request = await BindRequest<User>(null, new RequestOptions
			{
				QueryString = "?userId=user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72"
			});
			request.UserId.ToString().Should().Be("user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72");
		}

		[Fact]
		public async Task IdInRoute()
		{
			var request = await BindRequest<User>(null, new RequestOptions
			{
				RouteValues = new RouteValueDictionary
				{
					{"userId", "user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72" }
				}
			});
			request.UserId.ToString().Should().Be("user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72");
		}

		[Fact]
		public async Task IdRequest()
		{
			var request = await BindRequest<User>("{ \"userId\": \"user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72\"}");
			request.UserId.ToString().Should().Be("user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72");
		}

		[Fact]
		public async Task KitchenSink()
		{
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
			var request = await BindRequest<Request>(data, new RequestOptions
			{
				QueryString = "?value=7&implicitQueryValue=11",
				RouteValues = new RouteValueDictionary
				{
					{ "routeValue", 14 },
					{ "implicitRouteValue", 22 }
				}
			});
			request.Complex.Boolean.Should().Be(data.Complex.Boolean);
			request.Complex.String.Should().Be(data.Complex.String);
			request.Int.Should().Be(data.Int);
			request.String.Should().Be(data.String);
			request.QueryValue.Should().Be(7);
			request.ImplicitQueryValue.Should().Be(11);
			request.RouteValue.Should().Be(14);
			request.ImplicitRouteValue.Should().Be(22);
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

		[Fact]
		public async Task NullableIdInQuery()
		{
			var request = await BindRequest<NullableIds>(null, new RequestOptions
			{
				QueryString = "?id=7"
			});
			request.Id.Should().Be(7);
		}

		[Fact]
		public async Task NullableIdInRoute()
		{
			var request = await BindRequest<NullableIds>(null, new RequestOptions
			{
				RouteValues = new RouteValueDictionary
				{
					{"id", 7 }
				}
			});
			request.Id.Should().Be(7);
		}

		[Fact]
		public async Task ParseBodyIfNotFoundAnywhere()
		{
			await BindRequest<User>("{\"userId\": \"user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72\"}");
			httpContext.Request.Body.Invoking(b => b.Position).Should().Throw<ObjectDisposedException>();
		}

		[Fact]
		public async Task SomeIdRequests()
		{
			var request = await BindRequest<SomeIds>("{ \"UserId1\": \"user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72\", \"UserId2\": \"user.e98f7c99-45d4-449b-97f2-670cc90d9f08\"}");
			request.UserId1.ToString().Should().Be("user.9f6f3a93-4d61-48a4-b222-c6b3917f1f72");
			request.UserId2.ToString().Should().Be("user.e98f7c99-45d4-449b-97f2-670cc90d9f08");
		}

		private Task<T> BindRequest<T>(object data, RequestOptions options = null)
		{
			return BindRequest<T>(JsonSerializer.Serialize(data), options);
		}

		private Task<T> BindRequest<T>(string json, RequestOptions options = null)
		{
			options ??= new RequestOptions();
			var writer = new StreamWriter(bodyStream);
			writer.Write(json);
			writer.Flush();
			bodyStream.Position = 0;
			if (options.RouteValues != null)
			{
				httpContext.Request.RouteValues = options.RouteValues;
			}
			httpContext.Request.Body = bodyStream;
			if (options.QueryString != null)
			{
				httpContext.Request.QueryString = new QueryString(options.QueryString);
			}
			httpContext.Request.ContentType = "application/json";
			return binder.Bind<T>(httpContext);
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

		public class NullableIds
		{
			public int? Id { get; set; }
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
			public int ImplicitQueryValue { get; set; }
			public int ImplicitRouteValue { get; set; }
			public int Int { get; set; }
			public long Long { get; set; }
			public string NotExist { get; set; }

			[Api.FromQuery("value")]
			public int QueryValue { get; set; }

			[Api.FromRoute("routeValue")]
			public int RouteValue { get; set; }

			public sbyte SByte { get; set; }
			public short Short { get; set; }
			public string String { get; set; }

			[JsonConverter(typeof(CustomTimeSpanConverter))]
			public TimeSpan TimeSpan { get; set; }

			public uint UInt { get; set; }
			public ulong ULong { get; set; }
			public Uri Uri { get; set; }
			public ushort UShort { get; set; }

			[JsonConverter(typeof(CustomVersionConverter))]
			public Version Version { get; set; }
		}

		public class RequestOptions
		{
			public string QueryString { get; set; }
			public RouteValueDictionary RouteValues { get; set; }
		}

		public class SomeIds
		{
			public Id<User> UserId1 { get; set; }
			public Id<User> UserId2 { get; set; }
		}

		private class EmptyRequest { }
	}

	public class JsonConfigureOptions : IConfigureOptions<JsonOptions>
	{
		public void Configure(JsonOptions options)
		{
			options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.JsonSerializerOptions.Converters.Add(new StrongIdValueConverterFactory());
		}
	}
}