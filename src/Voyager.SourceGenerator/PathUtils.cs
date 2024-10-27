using System.Text;

namespace Voyager.SourceGenerator;

public static class PathUtils
{
	public static string ToName(string path, string suffix, string? prefix = null)
	{
		var capitalize = true;
		var skipping = 0;
		var name = new StringBuilder();
		if (prefix != null)
		{
			name.Append(char.ToUpper(prefix[0]));
			name.Append(prefix.Substring(1));
		}
		foreach (var @char in path)
		{
			switch (@char)
			{
				case '/':
					capitalize = true;
					break;

				case '{':
					skipping++;
					break;

				case '}':
					skipping--;
					break;

				default:
					if (skipping > 0)
					{
						break;
					}
					if (capitalize)
					{
						name.Append(char.ToUpper(@char));
						capitalize = false;
					}
					else
					{
						name.Append(@char);
					}
					break;
			}
			if (@char == '/')
			{
				capitalize = true;
			}
		}
		name.Append(suffix);
		return name.ToString();
	}
}
