namespace Voyager.SourceGenerator;

internal class InstanceInfo(string code)
{
	public string Code => code;

	public bool IsValidationResult { get; set; } = false;

	public override string ToString()
	{
		return Code;
	}
}
