using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.TestEndpoint;
using System.Net;

namespace IntegrationTests;

public class Tests
{
	private readonly HttpClient client;
	public Tests()
	{
		client = new WebApplicationFactory<MinimalApi.Program>().CreateClient();
	}

	[Fact]
	public async Task PropertyInjection()
	{
		var response = await client.GetAsync("/propertyInjection");
		response.Should().Be200Ok();
	}

	[Fact]
	public async Task FluentValidationInvalid()
	{
		var response = await client.PostAsJsonAsync("/validation/fluent", new { });
		response.Should().Be400BadRequest();
	}

	[Fact]
	public async Task FluentValidationValid()
	{
		var response = await client.PostAsJsonAsync("/validation/fluent", new { Text = "hi" });
		response.Should().Be200Ok();
	}

	[Fact]
	public async Task DefaultValidationInvalid()
	{
		var response = await client.PostAsJsonAsync("/validation/default", new { });
		var str = await response.Content.ReadAsStringAsync();
		response.Should().Be400BadRequest();
	}

	[Fact]
	public async Task DefaultValidationValid()
	{
		var response = await client.PostAsJsonAsync("/validation/default", new { Text = "hi" });
		response.Should().Be200Ok();
	}

	[Fact]
	public async Task ValidotValidationInvalid()
	{
		var response = await client.PostAsJsonAsync("/validation/validot", new { });
		response.Should().Be400BadRequest();
	}

	[Fact]
	public async Task ValidotValidationValid()
	{
		var response = await client.PostAsJsonAsync("/validation/validot", new { Text = "hi" });
		response.Should().Be200Ok();
	}

	[Fact]
	public async Task ValidationByHandlerValid()
	{
		var response = await client.PostAsJsonAsync("/validation/handledByHandler", new { });
		response.Should().Be200Ok();
	}

	[Fact]
	public async Task Configured()
	{
		var response = await client.GetAsync("/configured");
		response.Should().Be404NotFound();
	}

	[Fact]
	public async Task CancelToken()
	{
		var response = await client.GetAsync("/cancelToken");
		response.Should().Be200Ok().And.BeAs(new
		{
			Success = true
		});
	}

	[Fact]
	public async Task AnonymousResponse1()
	{
		var response = await client.PostAsJsonAsync("/anonymousResponse", new { });
		response.Should().Be200Ok().And.Satisfy<AnonymousResponse.ObjectResponse>(
			r => r.Text.Should().Be("1"));
	}

	[Fact]
	public async Task AnonymousResponse2()
	{
		var response = await client.PostAsJsonAsync("/anonymousResponse", new { Test = "hi" });
		response.Should().Be200Ok().And.Satisfy<AnonymousResponse.ObjectResponse>(
			r => r.Text.Should().Be("2"));
	}

	[Fact]
	public async Task MultipleInjections()
	{
		var response = await client.GetAsync("/multipleInjections");
		response.Should().Be200Ok();

		var postResponse = await client.PostAsJsonAsync("/multipleInjections", new { });
		response.Should().Be200Ok();
	}

	[Fact]
	public async Task Records()
	{
		var request = new
		{
			Value = 3,
			Text = "abc",
			Name = "def",
			policy = new Policy { Rules = [new Rule { Value = 3 }] }
		};
		var response = await client.PostAsJsonAsync("/records?id=something", request);
		response.Should().Be200Ok().And.BeAs(new
		{
			value = $"something 3 def 1 3"
		});
	}

	[Theory]
	[InlineData(101, HttpStatusCode.InternalServerError)]
	[InlineData(2, HttpStatusCode.NotFound)]
	[InlineData(11, HttpStatusCode.BadRequest)]
	[InlineData(1, HttpStatusCode.OK)]
	public async Task Weather(int days, HttpStatusCode statusCode)
	{
		var response = await client.GetAsync($"v2/WeatherForecast/Raymore?d={days}");
		response.Should().HaveStatusCode(statusCode);
	}

	[Fact]
	public async Task OpenApi()
	{
		var response = await client.GetAsync("openapi/v1.json");
		response.Should().Be200Ok().And.BeAs(new
		{
			components = new
			{
				schemas = new
				{
					postAnonymousResponseResponse = new
					{
						type = "object",
						oneOf = (object[])[new { }, new { }]
					}
				}
			}
		});
	}
}
