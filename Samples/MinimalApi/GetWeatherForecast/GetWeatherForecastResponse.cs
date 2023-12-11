namespace Shared.GetWeatherForecast
{
	public class GetWeatherForecastResponse
	{
		public required string City { get; set; }
		public DateTime Date { get; set; }

		public required string Summary { get; set; }
		public int TemperatureC { get; set; }

		public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
	}
}