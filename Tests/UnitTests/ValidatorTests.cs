using FluentValidation;

namespace UnitTests
{
	public class ValidatorTests
	{
		[Fact]
		public async Task ValidateName()
		{
			var validator = new VoyagerApi_EndpointPostValidator();
			var request = new VoyagerApi.Request();
			var result = await validator.ValidateAsync(request);
			var dict = result.ToDictionary();
			dict.ReplaceKey("UserId", "id");
		}

		private class VoyagerApi_EndpointPostValidator : AbstractValidator<VoyagerApi.Request>
		{
			public VoyagerApi_EndpointPostValidator()
			{
				VoyagerApi.Request.Validate(this);
			}
		}
	}

	public static class DictionaryExt
	{
		public static void ReplaceKey(this IDictionary<string, string[]> dictionary, string oldKey, string newKey)
		{
			if (dictionary.TryGetValue(oldKey, out var value))
			{
				dictionary.Remove(oldKey);
				dictionary.TryAdd(newKey, value);
			}
		}
	}
}
