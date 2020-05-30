using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;

namespace Voyager
{
	public class BoundProperty
	{
		public BindingSource BindingSource { get; set; }
		public string Description { get; set; }
		public string Name { get; set; }
		public PropertyInfo Property { get; set; }
		public string PropertyName { get; set; }
	}
}