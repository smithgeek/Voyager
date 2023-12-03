using System;

namespace Voyager;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class VoyagerEndpointAttribute : Attribute
{
	public VoyagerEndpointAttribute(string path)
	{
		Path = path;
	}

	public string Path { get; }
}