using System;
using System.Collections.Generic;
using System.Linq;

namespace Voyager.OpenApi;

public interface ISchemaIdGenerator
{
	string GetId<Type>();

	string GetId(Type type);

	void AddTypes(IEnumerable<Type> types);
}

public class SchemaIdGenerator : ISchemaIdGenerator
{
	private readonly Dictionary<Type, string> ids = new();

	public string GetId(Type type)
	{
		if (ids.TryGetValue(type, out var id))
		{
			return id;
		}
		throw new InvalidOperationException();
	}

	public string GetId<Type>()
	{
		return GetId(typeof(Type));
	}

	public void AddTypes(IEnumerable<Type> types)
	{
		var groups = types.GroupBy(t => t.Name);
		foreach (var group in groups)
		{
			if (group.Count() > 1)
			{
				foreach (var type in group)
				{
					ids[type] = type.FullName ?? throw new NotImplementedException();
				}
			}
			else
			{
				ids[group.First()] = group.Key;
			}
		}
	}
}