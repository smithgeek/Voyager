using Microsoft.AspNetCore.Mvc;
using System;

namespace Voyager.Api
{
	public class ProblemDetailsException : Exception
	{
		public ProblemDetailsException(ProblemDetails problem)
		{
			Problem = problem;
		}

		public ProblemDetails Problem { get; }
	}
}