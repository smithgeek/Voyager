using System.Collections.Generic;

namespace Voyager.Mediatr
{
	internal interface PolicyList
	{
		IEnumerable<string> PolicyNames { get; set; }
	}
}