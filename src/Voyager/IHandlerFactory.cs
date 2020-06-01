using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Voyager.UnitTests")]

namespace Voyager
{
	internal interface IHandlerFactory
	{
		object CreateInstance();
	}
}