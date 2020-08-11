using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Voyager.Api.Authorization;
using Voyager.Middleware;
using Xunit;

namespace Voyager.UnitTests
{
	public class AuthenticatedPolicyOverride : OverridePolicy<AuthenticatedPolicy>
	{
		public IList<IAuthorizationRequirement> GetRequirements()
		{
			return new[]
			{
				new CustomAppRequirement()
			};
		}
	}

	public class BlankPolicy : Policy
	{
		public IList<IAuthorizationRequirement> GetRequirements()
		{
			return new List<IAuthorizationRequirement>();
		}
	}

	public class CustomAppRequirement : IAuthorizationRequirement { }

	public class VoyagerStartupTests
	{
		[Fact]
		public void FindPolicies()
		{
			var definitions = VoyagerStartup.GetPolicies(new[] { typeof(BlankPolicy).Assembly });
			definitions.Count().Should().Be(2);
		}

		[Fact]
		public void PoliciesCanBeOverwritten()
		{
			OverwritePolicy(new[] { typeof(AuthenticatedPolicyOverride).Assembly, typeof(AuthenticatedPolicy).Assembly });
		}

		[Fact]
		public void PoliciesCanBeOverwrittenReverseAssemblyOrder()
		{
			OverwritePolicy(new[] { typeof(AuthenticatedPolicy).Assembly, typeof(AuthenticatedPolicyOverride).Assembly });
		}

		[Fact]
		public void RunningStartupMultipleTimesIsOk()
		{
			var services = new ServiceCollection();
			services.AddVoyager();
			services.AddVoyager(c => c.AddAssemblyWith<BlankPolicy>());
			var instance = services.BuildServiceProvider().GetService<ExceptionHandler>();
			instance.Should().NotBeNull();
		}

		private void OverwritePolicy(IEnumerable<Assembly> assemblies)
		{
			var expectedRequirements = new AuthenticatedPolicyOverride().GetRequirements();
			var definitions = VoyagerStartup.GetPolicies(assemblies);
			definitions.Should().Contain(def => def.Name == typeof(AuthenticatedPolicy).FullName);
			var actualRequirements = definitions.FirstOrDefault(def => def.Name == typeof(AuthenticatedPolicy).FullName).Policy.GetRequirements();
			actualRequirements.Count.Should().Be(1);
			actualRequirements.First().GetType().Should().Be<CustomAppRequirement>();
			definitions.Should().NotContain(def => def.Name == typeof(AuthenticatedPolicyOverride).FullName);
		}
	}
}