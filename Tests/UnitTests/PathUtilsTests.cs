using Voyager.SourceGenerator;

namespace UnitTests;

public class PathUtilsTests
{
	[Theory]
	[InlineData("/validot/benchmark/ok/{id}", "Request", "ValidotBenchmarkOkRequest")]
	[InlineData("duplicate/anonymous", "Request", "DuplicateAnonymousRequest")]
	public void ConvertPathToName(string path, string suffix, string expected)
	{
		var name = PathUtils.ToName(path, suffix);
		Assert.Equal(expected, name);
	}
}
