using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Voyager.Api;

[assembly: InternalsVisibleTo("Voyager.UnitTests")]

namespace Voyager
{
	public class HandlerFactory<THandler> : IHandlerFactory
	{
		private readonly IEnumerable<string> policyNames;
		private readonly IServiceProvider provider;

		public HandlerFactory(IServiceProvider provider, IEnumerable<string> policyNames)
		{
			this.provider = provider;
			this.policyNames = policyNames;
		}

		public THandler Create()
		{
			var instance = provider.GetRequiredService<THandler>();
			var injectable = (InjectEndpointProps)instance;
			injectable.HttpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
			injectable.AuthorizationService = provider.GetRequiredService<IAuthorizationService>();
			injectable.PolicyNames = policyNames;
			return instance;
		}

		object IHandlerFactory.CreateInstance()
		{
			return Create();
		}
	}
}