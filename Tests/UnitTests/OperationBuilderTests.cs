using Microsoft.OpenApi.Models;
using Voyager.OpenApi;

namespace Voyager
{

	public class OperationBuilderTests
	{
		private readonly IOpenApiSchemaGenerator schemaGenerator;
		private readonly OperationBuilder operationBuilder;

		public OperationBuilderTests()
		{
			schemaGenerator = OpenApiSchemaGenerator.GetSchemaGenerator();
			operationBuilder = new OperationBuilder(schemaGenerator, new OpenApiOperation());
		}

		public class MyRequestBodyClass
		{
		}

		public class MyResponseClass
		{

		}

		public class ErrorResponseClass { }

		[Fact]
		public void AddBody_ShouldSetRequestBodyWithCorrectContent()
		{
			// Arrange
			var bodyType = typeof(MyRequestBodyClass);

			// Act
			operationBuilder.AddBody(bodyType);
			var operation = operationBuilder.Build();

			// Assert
			Assert.NotNull(operation.RequestBody);
			Assert.Single(operation.RequestBody.Content);
			Assert.Contains("application/json", operation.RequestBody.Content.Keys);
			Assert.NotNull(operation.RequestBody.Content["application/json"].Schema);
		}

		[Fact]
		public void AddParameter_ShouldAddParameterToOperation()
		{
			// Arrange
			var name = "param";
			var location = ParameterLocation.Query;
			var type = typeof(int);
			var required = true;

			// Act
			operationBuilder.AddParameter(name, location, type, required);
			var operation = operationBuilder.Build();

			// Assert
			Assert.Single(operation.Parameters);
			var parameter = operation.Parameters[0];
			Assert.Equal(name, parameter.Name);
			Assert.Equal(location, parameter.In);
			Assert.NotNull(parameter.Schema);
			Assert.Equal(required, parameter.Required);
		}

		[Fact]
		public void AddResponse_ShouldAddResponseToOperation()
		{
			// Arrange
			var statusCode = 200;
			var responseType = typeof(MyResponseClass);

			// Act
			operationBuilder.AddResponse(statusCode, responseType);
			var operation = operationBuilder.Build();

			// Assert
			Assert.Single(operation.Responses);
			Assert.Contains(statusCode.ToString(), operation.Responses.Keys);
			var response = operation.Responses[statusCode.ToString()];
			Assert.Single(response.Content);
			Assert.Contains("application/json", response.Content.Keys);
			Assert.NotNull(response.Content["application/json"].Schema);
		}

		[Fact]
		public void Build_ShouldReturnOperationWithCorrectResponses()
		{
			// Arrange
			operationBuilder.AddResponse(200, typeof(MyResponseClass));
			operationBuilder.AddResponse(400, typeof(ErrorResponseClass));

			// Act
			var result = operationBuilder.Build();

			// Assert
			Assert.Equal(2, result.Responses.Count);
			Assert.Contains("200", result.Responses.Keys);
			Assert.Contains("400", result.Responses.Keys);
		}
	}
}
