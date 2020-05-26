using FluentValidation;

namespace DemoFunctionsApp.Sample
{
	public class SampleRequestValidator : AbstractValidator<SampleRequest>
	{
		public SampleRequestValidator()
		{
			RuleFor(r => r.Value).NotEqual("invalid");
			RuleFor(r => r.Number).NotEqual(10);
		}
	}
}