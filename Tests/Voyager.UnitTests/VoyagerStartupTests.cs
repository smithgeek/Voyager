using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Voyager.Api.Authorization;
using Xunit;

namespace Voyager.UnitTests
{
	public class BlankPolicy : Policy
	{
		public IList<IAuthorizationRequirement> GetRequirements()
		{
			return new List<IAuthorizationRequirement>();
		}
	}

	public class VoyagerStartupTests
	{
		[Fact]
		public void RunningStartupMultipleTimesIsOk()
		{
			var services = new ServiceCollection();
			services.AddVoyager();
			services.AddVoyager(c => c.AddAssemblyWith<BlankPolicy>());
			var policy = services.BuildServiceProvider().GetService<BlankPolicy>();
			policy.Should().NotBeNull();
		}
	}
}