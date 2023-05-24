using BlazorGrpcWebCodeFirstNet7.Shared;

namespace BlazorGrpcWebCodeFirstNet7.Server.Facades
{
	public class WeatherForecastFacade : IWeatherForecastFacade
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		public Task<List<WeatherForecast>> GetForecastAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateTime.Today.AddDays(index),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			})
			.ToList());
		}
	}
}
