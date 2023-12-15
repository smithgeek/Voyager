using System.Text.Json.Serialization;

namespace Voyager.ModelBinding.Tests
{
	public class ModelBinderTests
	{
		[Fact]
		public void GetBool_ShouldReturnTrue_WhenValueIsTrue()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetBool(new("true"));

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void GetBool_ShouldReturnDefaultValue_WhenValueIsMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetBool(new(), defaultValue: true);

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void GetBool_ShouldReturnFalse_WhenValueIsMissingAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetBool(new());

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void GetString_ShouldReturnCorrectValue_WhenValueExists()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetString(new("value"));

			// Assert
			Assert.Equal("value", result);
		}

		[Fact]
		public void GetString_ShouldReturnDefaultValue_WhenValueIsMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetString(new(), defaultValue: "default");

			// Assert
			Assert.Equal("default", result);
		}

		[Fact]
		public void GetString_ShouldReturnEmpty_WhenValueIsMissingAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetString(new());

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void GetNumber_ShouldReturnCorrectValue_WhenValueIsValid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumber<int>(new("123"));

			// Assert
			Assert.Equal(123, result);
		}

		[Fact]
		public void GetNumber_ShouldReturnDefaultValue_WhenValueIsInvalid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumber<int>(new("abc"), defaultValue: 0);

			// Assert
			Assert.Equal(0, result);
		}

		[Fact]
		public void GetObject_ShouldReturnCorrectValue_WhenValueIsValid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetObject<Person>(new("{\"name\":\"John\",\"age\":30}"), new());

			// Assert
			Assert.NotNull(result);
			Assert.Equal("John", result.Name);
			Assert.Equal(30, result.Age);
		}

		[Fact]
		public void GetObject_ShouldReturnDefaultValue_WhenValueIsInvalid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetObject<Person>(new("invalid"), new(), defaultValue: new Person { Name = "Default", Age = 0 });

			// Assert
			Assert.NotNull(result);
			Assert.Equal("Default", result.Name);
			Assert.Equal(0, result.Age);
		}

		[Fact]
		public void GetObject_ShouldReturnNull_WhenValueIsInvalidAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetObject<Person>(new("invalid"), new());

			// Assert
			Assert.Null(result);
		}

		public class Person
		{
			[JsonPropertyName("name")]
			public required string Name { get; set; }
			[JsonPropertyName("age")]
			public int Age { get; set; }
		}

		[Fact]
		public void TryGetBool_ShouldReturnTrue_WhenValueIsTrue()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetBool(new("true"), out var result);

			// Assert
			Assert.True(success);
			Assert.True(result);
		}

		[Fact]
		public void TryGetBool_ShouldReturnFalse_WhenValueIsInvalidAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetBool(new("abc"), out var result);

			// Assert
			Assert.False(success);
			Assert.False(result);
		}

		[Fact]
		public void TryGetBool_ShouldReturnFalse_WhenValueIsNull()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetBool(new((string?)null), out var result);

			// Assert
			Assert.False(success);
			Assert.False(result);
		}

		[Fact]
		public void TryGetBool_ShouldReturnDefault_WhenValueIsInvalid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetBool(new("abc"), out var result, defaultValue: true);

			// Assert
			Assert.True(success);
			Assert.True(result);
		}

		[Fact]
		public void GetBoolEnumerable_ShouldReturnCorrectValues_WhenValuesExist()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetBoolEnumerable(new(["true", "false", "true"]));

			// Assert
			Assert.Equal([true, false, true], result);
		}

		[Fact]
		public void GetBoolEnumerable_ShouldReturnEmpty_WhenValuesAreMissingAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetBoolEnumerable(new());

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void GetBoolEnumerable_ShouldReturnDefault_WhenValuesAreMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetBoolEnumerable(new(), new DefaultValue<IEnumerable<bool>>([true, false]));

			// Assert
			Assert.Equal([true, false], result);
		}

		[Fact]
		public void TryGetString_ShouldReturnTrue_WhenValueExists()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetString(new("value"), out var result);

			// Assert
			Assert.True(success);
			Assert.Equal("value", result);
		}

		[Fact]
		public void TryGetString_ShouldReturnFalse_WhenValueIsMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetString(new(), out var result);

			// Assert
			Assert.False(success);
			Assert.Equal(result, string.Empty);
		}

		[Fact]
		public void TryGetString_ShouldReturnDefault_WhenValueIsMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetString(new(), out var result, "default");

			// Assert
			Assert.True(success);
			Assert.Equal("default", result);
		}

		[Fact]
		public void GetStringEnumerable_ShouldReturnCorrectValues_WhenValuesExist()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetStringEnumerable(new(["value1", "value2", "value3"]));

			// Assert
			Assert.Equal(["value1", "value2", "value3"], result);
		}

		[Fact]
		public void GetStringEnumerable_ShouldReturnEmpty_WhenValuesAreMissingAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetStringEnumerable(new());

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void GetStringEnumerable_ShouldReturnDefault_WhenValuesAreMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetStringEnumerable(new(), new DefaultValue<IEnumerable<string>>(["a", "1", "b"]));

			// Assert
			Assert.Equal(["a", "1", "b"], result);
		}

		[Fact]
		public void GetStringEnumerable_ShouldReturnEmpty_WhenValueIsNull()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetStringEnumerable(new((string?)null));

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void GetNumberEnumerable_ShouldReturnCorrectValues_WhenValuesExist()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumberEnumerable<int>(new(["1", "2", "3"]));

			// Assert
			Assert.Equal([1, 2, 3], result);
		}

		[Fact]
		public void GetNumberEnumerable_ShouldReturnEmpty_WhenValuesAreMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumberEnumerable<int>(new());

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void GetNumberEnumerable_ShouldReturnOnlyNumbers_WhenSomeValueAreInvalid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumberEnumerable<int>(new(["1", "abc", "3"]));

			// Assert
			Assert.Equal([1, 3], result);
		}

		[Fact]
		public void GetNumberEnumerable_ShouldReturnDefault_WhenValuesAreMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumberEnumerable<int>(new(), new DefaultValue<IEnumerable<int>>([1, 4, 6]));

			// Assert
			Assert.Equal([1, 4, 6], result);
		}

		[Fact]
		public void TryGetNumber_ShouldReturnTrue_WhenValueIsValid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetNumber<int>(new("123"), out var result);

			// Assert
			Assert.True(success);
			Assert.Equal(123, result);
		}

		[Fact]
		public void TryGetNumber_ShouldReturnFalse_WhenValueIsInvalidAndNoDefault()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetNumber<int>(new("abc"), out var result);

			// Assert
			Assert.False(success);
			Assert.Equal(0, result);
		}

		[Fact]
		public void TryGetNumber_ShouldReturnFalse_WhenValueIsNull()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetNumber<int>(new(), out var result);

			// Assert
			Assert.False(success);
			Assert.Equal(0, result);
		}

		[Fact]
		public void TryGetNumber_ShouldReturnDefault_WhenValueIsInvalid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var success = binder.TryGetNumber<int>(new("abc"), out var result, 24);

			// Assert
			Assert.True(success);
			Assert.Equal(24, result);
		}

		[Fact]
		public void GetNumber_ShouldReturnValue_WhenValueIsValid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumber<int>(new("123"));

			// Assert
			Assert.Equal(123, result);
		}

		[Fact]
		public void GetNumber_ShouldReturnDefault_WhenValueIsInvalid()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumber<int>(new("abc"));
			// Assert
			Assert.Equal(0, result);
		}

		[Fact]
		public void GetNumber_ShouldReturnDefault_WhenValueIsMissing()
		{
			// Arrange
			var binder = new ModelBinder();

			// Act
			var result = binder.GetNumber<int>(new(), 7);
			// Assert
			Assert.Equal(7, result);
		}
	}
}