﻿#pragma warning disable CA1822
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

namespace Runner;

[MemoryDiagnoser, SimpleJob(launchCount: 1, warmupCount: 5, iterationCount: 50, invocationCount: 10000)]
public class Benchmarks
{
	const string QueryObjectParams = "?id=101&FirstName=Name&LastName=LastName&Age=23&phoneNumbers[0]=223422&phonenumbers[1]=11144" +
									 "&NestedQueryObject.id=101&NestedQueryObject.FirstName=Name&NestedQueryObject.LastName=LastName&NestedQueryObject.Age=23&NestedQueryObject.phoneNumbers[0]=223422&NestedQueryObject.phonenumbers[1]=1114" +
									 "&NestedQueryObject.MoreNestedQueryObject.id=101&NestedQueryObject.MoreNestedQueryObject.FirstName=Name&NestedQueryObject.MoreNestedQueryObject.LastName=LastName" +
									 "&NestedQueryObject.MoreNestedQueryObject.Age=23&NestedQueryObject.MoreNestedQueryObject.phoneNumbers[0]=223422&NestedQueryObject.MoreNestedQueryObject.phonenumbers[1]=1114";

	static HttpClient FastEndpointClient { get; } = new WebApplicationFactory<FEBench.Program>().CreateClient();
	static HttpClient FeCodeGenClient { get; } = new WebApplicationFactory<FEBench.Program>().CreateClient();
	static HttpClient FeScopedValidatorClient { get; } = new WebApplicationFactory<FEBench.Program>().CreateClient();
	static HttpClient FeThrottleClient { get; } = new WebApplicationFactory<FEBench.Program>().CreateClient();
	public static HttpClient MinimalClient { get; } = new WebApplicationFactory<MinimalApi.Program>().CreateClient();
	static HttpClient MvcClient { get; } = new WebApplicationFactory<MvcControllers.Program>().CreateClient();
	static HttpClient VoyagerClient { get; } = new WebApplicationFactory<VoyagerApi.Program>().CreateClient();

	static readonly StringContent _payload = new(
		JsonSerializer.Serialize(
			new
			{
				FirstName = "xxx",
				LastName = "yyy",
				Age = 23,
				PhoneNumbers = new[]
				{
					"1111111111",
					"2222222222",
					"3333333333",
					"4444444444",
					"5555555555"
				}
			}),
		Encoding.UTF8,
		"application/json");

	[Benchmark(Baseline = true)]
	public async Task Voyager()
	{
		await VoyagerClient.SendAsync(new()
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{VoyagerClient.BaseAddress}benchmark/ok/123"),
			Content = _payload
		});
	}

	[Benchmark]
	public async Task VoyagerValidot()
	{
		await VoyagerClient.SendAsync(new()
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{VoyagerClient.BaseAddress}/validot/benchmark/ok/123"),
			Content = _payload
		});
	}

	[Benchmark]
	public Task MinimalApi()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{MinimalClient.BaseAddress}benchmark/ok/123"),
			Content = _payload
		};

		return MinimalClient.SendAsync(msg);

	}

	[Benchmark]
	public Task AspNetCoreMvc()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{MvcClient.BaseAddress}benchmark/ok/123"),
			Content = _payload
		};

		return MvcClient.SendAsync(msg);
	}



	//[Benchmark]
	public Task VoyagerTests()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{VoyagerClient.BaseAddress}manual/benchmark/ok/123"),
			Content = _payload
		};

		return VoyagerClient.SendAsync(msg);
	}

	[Benchmark]
	public Task FastEndpoints()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{FastEndpointClient.BaseAddress}benchmark/ok/123"),
			Content = _payload
		};

		return FastEndpointClient.SendAsync(msg);
	}




	[Benchmark]
	public Task FastEndpointsCodeGen()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{FeCodeGenClient.BaseAddress}benchmark/codegen/123"),
			Content = _payload
		};

		return FeCodeGenClient.SendAsync(msg);
	}

	//[Benchmark]
	public Task FastEndpointsScopedValidator()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{FeScopedValidatorClient.BaseAddress}benchmark/scoped-validator/123"),
			Content = _payload
		};

		return FeScopedValidatorClient.SendAsync(msg);
	}


	//[Benchmark]
	public Task FastEndpointsThrottling()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Post,
			RequestUri = new($"{FeThrottleClient.BaseAddress}benchmark/throttle/123"),
			Content = _payload
		};
		msg.Headers.Add("X-Forwarded-For", $"000.000.000.{Random.Shared.NextInt64(100, 200)}");

		return FeThrottleClient.SendAsync(msg);
	}

	//[Benchmark]
	public Task FastEndpointsQueryBinding()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Get,
			RequestUri = new($"{FastEndpointClient.BaseAddress}benchmark/query-binding{QueryObjectParams}")
		};

		return FastEndpointClient.SendAsync(msg);
	}

	//[Benchmark(Baseline = true)]
	public Task AspNetCoreMvcQueryBinding()
	{
		var msg = new HttpRequestMessage
		{
			Method = HttpMethod.Get,
			RequestUri = new($"{MvcClient.BaseAddress}benchmark/query-binding{QueryObjectParams}")
		};

		return MvcClient.SendAsync(msg);
	}
}