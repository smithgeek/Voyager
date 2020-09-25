namespace Voyager
{
	public class VoyagerMapOptions
	{
		private string prefix = null;

		public string Prefix
		{
			get
			{
				if (string.IsNullOrWhiteSpace(prefix))
				{
					return string.Empty;
				}
				return prefix + "/";
			}
			set { prefix = value?.TrimEnd('/'); }
		}
	}
}