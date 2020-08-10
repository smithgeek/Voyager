using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;
using Voyager.Middleware;
using Xunit;

namespace Voyager.UnitTests.Middleware
{
	public class VoyagerExceptionHandlerMiddlewareTests
	{
		[Fact]
		public async Task ConfiguratorIsCalled()
		{
			var configurator = new TestExceptionConfigurator();
			var middleware = new VoyagerExceptionHandlerMiddleware((context) => throw new Exception("Error"), new NullExceptionHandler(), new[] { configurator }, new NullLogger<VoyagerExceptionHandlerMiddleware>());
			await middleware.InvokeAsync(TestFactory.HttpContext());
			configurator.CalledCount.Should().Be(1);
		}

		public class NullExceptionHandler : ExceptionHandler
		{
			public IActionResult HandleException<TException>(TException exception) where TException : Exception
			{
				return new OkResult();
			}
		}

		public class TestExceptionConfigurator : ExceptionHandlerConfigurator
		{
			public int CalledCount { get; set; } = 0;

			public void Configure()
			{
				CalledCount++;
			}
		}
	}
}