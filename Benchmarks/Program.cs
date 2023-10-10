// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

BenchmarkRunner.Run<DeserializationTests>();

public class DeserializationTests
{
	[Benchmark]
	public async Task BodyReassign()
	{
		var stream = new MemoryStream(Encoding.ASCII.GetBytes("{\"name\": \"Bender\", \"age\": 32}"));
		var bodyData = await JsonSerializer.DeserializeAsync(stream, RequestSerializationContext.Default.BodyData);
		if (bodyData != null)
		{
			var _ = new RequestWithBody
			{
				Name = bodyData.Name,
				Age = bodyData.Age
			};
		}
	}

	[Benchmark]
	public async Task BodyReassignReflection()
	{
		var stream = new MemoryStream(Encoding.ASCII.GetBytes("{\"name\": \"Bender\", \"age\": 32}"));
		var bodyData = await JsonSerializer.DeserializeAsync<RequestWithBody.BodyData>(stream);
		if (bodyData != null)
		{
			var _ = new RequestWithBody
			{
				Name = bodyData.Name,
				Age = bodyData.Age
			};
		}
	}

	[Benchmark]
	public async Task BodyRecord()
	{
		var stream = new MemoryStream(Encoding.ASCII.GetBytes("{\"name\": \"Bender\", \"age\": 32}"));
		var _ = new RequestWithBody
		{
			Body = (await JsonSerializer.DeserializeAsync(stream, RequestSerializationContext.Default.BodyData))!
		};
	}

	[Benchmark]
	public async Task JsonElement()
	{
		var stream = new MemoryStream(Encoding.ASCII.GetBytes("{\"name\": \"Bender\", \"age\": 32}"));
		var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(stream);
		if (dictionary != null)
		{
			GetValue<string>(nameof(Request.Name), dictionary);
			GetValue<int>(nameof(Request.Age), dictionary);
		}
	}

	[Benchmark]
	public async Task RecordSourceGenerator()
	{
		var stream = new MemoryStream(Encoding.ASCII.GetBytes("{\"name\": \"Bender\", \"age\": 32}"));
		await JsonSerializer.DeserializeAsync(stream, RequestSerializationContext.Default.Request2);
	}

	[Benchmark]
	public async Task SourceGenerator()
	{
		var stream = new MemoryStream(Encoding.ASCII.GetBytes("{\"name\": \"Bender\", \"age\": 32}"));
		await JsonSerializer.DeserializeAsync(stream, RequestSerializationContext.Default.Request);
	}

	private TValue? GetValue<TValue>(string key, Dictionary<string, JsonElement> dictionary)
	{
		var jsonKey = JsonNamingPolicy.CamelCase.ConvertName(key);
		var kvp = dictionary.FirstOrDefault(kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key) == jsonKey);
		if (kvp.Key != null)
		{
			return kvp.Value.Deserialize<TValue>() ?? default;
		}
		return default;
	}
}

public record class Request2(int Age, string Name);

public class Request
{
	public int Age { get; init; }
	public string Name { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Request2))]
[JsonSerializable(typeof(RequestWithBody.BodyData))]
public partial class RequestSerializationContext : JsonSerializerContext
{ }

public class RequestWithBody
{
	public int? Age { get; set; }
	public BodyData? Body { get; init; }

	public string? Name { get; set; }

	public class BodyData
	{
		public int Age { get; set; }
		public string? Name { get; set; }
	}

	public record BodyType(string Name, int Age);
}