# gRPC code-first for Blazor WebAssembly front-end

gRPC is a phenomenon of our time. This modern and performance-efficient protocol is rapidly spreading, and today we will show how to use it for communication between the Blazor WebAssembly front-end and the ASP.NET Core backend (host):

1. We will efficiently use the possibilities of sharing code between the server and client part. We will use the code-first arrangement and put the "contract" (interface for the called service and data object definitions) into the assembly shared by both the server and client parts of the solution.
1. To overcome browser limitations, we will use the gRPC-Web extension. 

We will show the entire implementation on a simple example – we will use the default Blazor WebAssembly App template from Visual Studio (ASP.NET Core hosted, version of the .NET7 template) and we will convert the prepared Fetch data example, which uses the REST API in this template, to a gRPC-Web call using code-first.

Let's do this, it's just a few steps:

## 1. MyBlazorSolution.Server – Preparing ASP.NET Core host
First, we prepare the server-side infrastructure for gRPC. We will go directly to the version with the gRPC-Web extension with code-first support and install NuGet packages
* [Grpc.AspNetCore.Web](https://www.nuget.org/packages/Grpc.AspNetCore.Web)
* [protobuf-net.Grpc.AspNetCore](https://www.nuget.org/packages/protobuf-net.Grpc.AspNetCore/)

We register support services in dependency-injection in `Startup.cs`:
```csharp
builder.Services.AddCodeFirstGrpc(config => { config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal; });
```

We add gRPC middleware somewhere between `UseRouting()` and endpoint definition (before `MapXy()` methods):
```csharp
app.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
```

## 2. MyBlazorSolution.Shared – Service contract definition (code-first)
Now we define in the form of an interface what our service will look like. We will then use the interface on the server side (we will create its implementation) and on the client side (we will generate a gRPC client that will implement the interface and we will directly use it in our code via dependency injection).
Add to the project a NuGet package that allows us to decorate the interface with necessary attributes
* [System.ServiceModel.Primitives](https://www.nuget.org/packages/System.ServiceModel.Primitives/)

Find the example `WeatherForecast.cs` file from the project template. It contains the definition of the return data message, which the sample REST API now returns to us. We will convert this class into the following form:
```csharp
[DataContract]
public class WeatherForecast
{
    [DataMember(Order = 1)]
    public DateTime Date { get; set; }
 
    [DataMember(Order = 2)]
    public int TemperatureC { get; set; }
 
    [DataMember(Order = 3)]
    public string? Summary { get; set; }
 
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```
* We added the `[DataContract]` attribute to mark the class we will use as the gRPC data message.
* We added `[DataMember(Order = ...)]` attributes that mark the elements to be transmitted via gRPC (others are ignored, here `TemperatureF` is calculated and recalculated from other data on the client anytime). Each element needs to be set `Order`, which defines the fixed layout for the used [protobuf](https://protobuf.dev/) serialization.
* We replaced the original `DateOnly` type with `DateTime`. We have to stick to types supported by used protobuf serialization.

Next, we need to create an interface that will describe the whole service:
```csharp
[ServiceContract]
public interface IWeatherForecastFacade
{
    Task<List<WeatherForecast>> GetForecastAsync(CancellationToken cancellationToken = default);
}
```
* The `[ServiceContract]` attribute tells us the applicability for gRPC (can be used later for automatic registrations).
* By the nature of network communication, the entire interface should be asynchronous.
* We can use the optional `CancellationToken`, which can convey a signal of premature termination of communication by the client (or disconnection).

## 3. MyBlazorSolution.Server – Implementing the gRPC service
Now we need to implement the prepared interface on the server side (we will use slightly modified code from the sample `WeatherForecastController`, which you can now delete):
```csharp
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
```

Now we have to add the gRPC service in `Startup.cs`:
```csharp
app.MapGrpcService<WeatherForecastFacade>();
```

## 4. MyBlazorSolution.Client – gRPC client in Blazor WebAssembly
Now all that is left is to use the service in the Blazor WebAssembly front-end. The entire definition is available in the form of the `IWeatherForecastFacade` interface with its `WeatherForecast` data class.

We will add the necessary NuGet packages to the project:
* [Grpc.Net.Client.Web](https://www.nuget.org/packages/Grpc.Net.Client.Web)
* [protobuf-net.Grpc.ClientFactory](https://www.nuget.org/packages/protobuf-net.Grpc.ClientFactory)

Register the gRPC-Web infrastructure and the client (in factory form) in `Program.cs`:
```csharp
builder.Services.AddTransient<GrpcWebHandler>(provider => new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
 
builder.Services.AddCodeFirstGrpcClient<IWeatherForecastFacade>((provider, options) => =>
    {
        var navigationManager = provider.GetRequiredService<NavigationManager>();
        var backendUrl = navigationManager.BaseUri;
 
        options.Address = new Uri(backendUrl);
    })
    .ConfigurePrimaryHttpMessageHandler<GrpcWebHandler>();
```
Well, now we can use `IWeatherForecastFacade` anywhere in the front-end project by having the service injected using dependency injection. So, for example, we'll modify `FetchData.razor` to use our new gRPC service instead of the original REST API:
```razor
@inject IWeatherForecastFacade WeatherForecastFacade
 
...
 
@code {
    private List<WeatherForecast>? forecasts;
 
    protected override async Task OnInitializedAsync()
    {
        forecasts = await WeatherForecastFacade.GetForecastAsync();
    }
}
```
Done. The project should now be executable and the Fetch data page will now communicate via gRPC-Web.

You can check your solution against the sample repository
* [https://github.com/hakenr/BlazorGrpcWebCodeFirstNet7](https://github.com/hakenr/BlazorGrpcWebCodeFirstNet7)
* [sample commit](https://github.com/hakenr/BlazorGrpcWebCodeFirstNet7/commit/852d15ba670f7cd56fdd2c56c87faf1ece7ddffd)

## Other extensions and more advanced techniques
The gRPC service can, of course, accept input. In this case, use one input parameter for the incoming message - a data class created in the same way we prepared the WeatherForecast output. (Usually these classes are referred to as *Data Transfer Object* and thus given the suffix `Dto`. The implementation is usually as a C# `record`.)

If we have authentication and authorization in our project, then we can use the `[Authorize]` attribute on the implementing class/method, just as we would on a controller/action.

We can apply arbitrary techniques to the published gRPC endpoint like any other mapped server endpoint (rate limiting, caching, ...).

gRPC has support for *interceptors* which can be used to further improve gRPC communication
* pass exceptions from server to client (basic support is built-in, but you may want to enrich it with specific handling of custom scenarios),
* passing the required culture from client to server (what language the front-end is switched to),

In a more advanced variant of the layout, you can also provide automatic registration of interface and data contracts without having to decorate them with `[ServiceContract]`, `[DataContract]` and `[DataMember(Order = ...)]` attributes. All this and much more can be found ready in:
* the new project template [https://github.com/havit/NewProjectTemplate-Blazor](https://github.com/havit/NewProjectTemplate-Blazor)
* [Havit.Blazor libraries](https://havit.blazor.eu)

Both open-source with MIT license, free of charge.