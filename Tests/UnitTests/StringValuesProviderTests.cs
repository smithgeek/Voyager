using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Voyager.ModelBinding;

namespace Voyager.Tests.ModelBinding
{
	public class StringValuesProviderTests
	{
		private readonly HttpContext httpContext;
		private readonly StringValuesProvider stringValuesProvider;

		public StringValuesProviderTests()
		{
			httpContext = Substitute.For<HttpContext>();
			stringValuesProvider = new StringValuesProvider();
		}

		[Fact]
		public void GetStringValues_WithCookieSource_ShouldReturnCookieValue()
		{
			// Arrange
			var key = "myCookie";
			var expectedValue = "cookieValue";
			var cookies = Substitute.For<IRequestCookieCollection>();
			cookies.TryGetValue(key, out var value).Returns(callInfo =>
			{
				callInfo[1] = expectedValue;
				return true;
			});
			httpContext.Request.Cookies.Returns(cookies);

			// Act
			var result = stringValuesProvider.GetStringValues(httpContext, ModelBindingSource.Cookie, key);

			// Assert
			Assert.Equal(expectedValue, result);
		}

		[Fact]
		public void GetStringValues_WithRouteSource_ShouldReturnRouteValue()
		{
			// Arrange
			var key = "myRoute";
			var expectedValue = "routeValue";
			var context = new DefaultHttpContext();
			context.Request.RouteValues[key] = expectedValue;

			// Act
			var result = stringValuesProvider.GetStringValues(context, ModelBindingSource.Route, key);

			// Assert
			Assert.Equal(expectedValue, result);
		}

		[Fact]
		public void GetStringValues_WithFormSource_ShouldReturnFormValues()
		{
			// Arrange
			var key = "myForm";
			var expectedValues = new StringValues(["formValue1", "formValue2"]);
			var formCollection = Substitute.For<IFormCollection>();
			formCollection.TryGetValue(key, out var value).Returns(callInfo =>
			{
				callInfo[1] = expectedValues;
				return true;
			});
			httpContext.Request.Form.Returns(formCollection);

			// Act
			var result = stringValuesProvider.GetStringValues(httpContext, ModelBindingSource.Form, key);

			// Assert
			Assert.Equal(expectedValues, result);
		}

		[Fact]
		public void GetStringValues_WithHeaderSource_ShouldReturnHeaderValues()
		{
			// Arrange
			var key = "myHeader";
			var expectedValues = new StringValues(["headerValue1", "headerValue2"]);
			var context = new DefaultHttpContext();
			context.Request.Headers[key] = expectedValues;

			// Act
			var result = stringValuesProvider.GetStringValues(context, ModelBindingSource.Header, key);

			// Assert
			Assert.Equal(expectedValues, result);
		}

		[Fact]
		public void GetStringValues_WithQuerySource_ShouldReturnQueryValues()
		{
			// Arrange
			var key = "myQuery";
			var expectedValues = new StringValues(["queryValue1", "queryValue2"]);
			var queryCollection = Substitute.For<IQueryCollection>();
			queryCollection.TryGetValue(key, out var value).Returns(callInfo =>
			{
				callInfo[1] = expectedValues;
				return true;
			});
			httpContext.Request.Query.Returns(queryCollection);

			// Act
			var result = stringValuesProvider.GetStringValues(httpContext, ModelBindingSource.Query, key);

			// Assert
			Assert.Equal(expectedValues, result);
		}

		[Fact]
		public void GetStringValues_WithUnknownSource_ShouldReturnEmpty()
		{
			// Arrange
			var key = "myKey";

			// Act
			var result = stringValuesProvider.GetStringValues(httpContext, (ModelBindingSource)100, key);

			// Assert
			Assert.Equal(StringValues.Empty, result);
		}
	}
}