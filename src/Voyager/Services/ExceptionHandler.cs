using Microsoft.AspNetCore.Mvc;
using System;

namespace Voyager.Middleware
{
	public interface ExceptionHandler
	{
		IActionResult HandleException<TException>(TException exception) where TException : Exception;
	}
}