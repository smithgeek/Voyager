namespace Voyager.Api.Authorization
{
	public interface OverridePolicy<TPolicy> : Policy where TPolicy : Policy
	{

	}

}