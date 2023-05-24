using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BlazorGrpcWebCodeFirstNet7.Shared
{
	[ServiceContract]
	public interface IWeatherForecastFacade
	{
		Task<List<WeatherForecast>> GetForecastAsync(CancellationToken cancellationToken = default);
	}
}
