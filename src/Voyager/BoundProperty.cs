using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;

namespace Voyager
{
	public class BoundProperty
	{
		public BindingSource? BindingSource { get; set; }
		public required string Description { get; init; }
		public required string Name { get; set; }
		public required PropertyInfo Property { get; init; }
		public required string PropertyName { get; init; }
	}
}