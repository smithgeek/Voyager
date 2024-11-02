#pragma warning disable CA1822
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Voyager.Extensions;

namespace Runner;

[MemoryDiagnoser, SimpleJob(launchCount: 1, warmupCount: 10, iterationCount: 50, invocationCount: 300000)]
public class SpeedTests
{
	private FluentValidation.Results.ValidationResult validationResult = new();

	[GlobalSetup]
	public void Setup()
	{
		validationResult.Errors.Add(new("test", "some message"));
		validationResult.Errors.Add(new("UserId", "sdofinwe"));
		validationResult.Errors.Add(new("awoien", "aowienf"));
	}




	[Benchmark(Baseline = true)]
	public IResult OldWay()
	{
		var dictionary = validationResult.ToDictionary();
		dictionary.ReplaceKey("UserId", "id");
		return Results.ValidationProblem(dictionary);
	}

	[Benchmark]
	public IResult TestWay()
	{
		return Results.ValidationProblem(validationResult.Errors.GroupBy(x =>
		{
			return x.PropertyName switch
			{
				"UserId" => "id",
				_ => x.PropertyName
			};
		}).ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
	}
}
