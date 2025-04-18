namespace Voyager.Validation.FluentValidation;

public class VoyagerFluentValidationOptions
{
	public bool AutoRegister { get; set; } = true;
	public bool AddNotNullRuleForRequiredProperties { get; set; } = true;
}