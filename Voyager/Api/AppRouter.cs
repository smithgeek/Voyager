using MediatR;
using System;
using System.Threading.Tasks;

namespace Voyager.Api
{
	public class AppRouter
	{
		private readonly IMediator mediator;
		private readonly IServiceProvider serviceProvider;

		public AppRouter(IMediator mediator, IServiceProvider serviceProvider)
		{
			this.mediator = mediator;
			this.serviceProvider = serviceProvider;
		}

		public TRequest CreateRequest<TRequest>(Action<TRequest> initializer = null)
		{
			var request = (TRequest)serviceProvider.GetService(typeof(TRequest));
			initializer?.Invoke(request);
			return request;
		}

		public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
		{
			return mediator.Send(request);
		}
	}
}