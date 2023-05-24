using BlazorGrpcWebCodeFirstNet7.Client;
using BlazorGrpcWebCodeFirstNet7.Shared;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProtoBuf.Grpc.ClientFactory;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddTransient<GrpcWebHandler>(provider => new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));

builder.Services.AddCodeFirstGrpcClient<IWeatherForecastFacade>((provider, options) =>
	{
		var navigationManager = provider.GetRequiredService<NavigationManager>();
		var backendUrl = navigationManager.BaseUri;

		options.Address = new Uri(backendUrl);
	})
	.ConfigurePrimaryHttpMessageHandler<GrpcWebHandler>();

await builder.Build().RunAsync();
