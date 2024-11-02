namespace Voyager.SourceGenerator;

internal class InstanceInfo(string code)
{
	public string Code => code;

	public ValidationMode Flag { get; set; } = ValidationMode.None;

	public override string ToString()
	{
		return Code;
	}
}
