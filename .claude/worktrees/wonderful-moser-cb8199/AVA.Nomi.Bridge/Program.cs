using AVA.Nomi.Bridge;
using AVA.UPS.Adapter;
using AVA.UPS.Adapter.Routing;

var apiKey = Environment.GetEnvironmentVariable("NOMI_API_KEY")
             ?? throw new InvalidOperationException("NOMI_API_KEY environment variable is not set.");
var port = Environment.GetEnvironmentVariable("BRIDGE_PORT") ?? "8080";
var prefix = $"http://localhost:{port}/";

var client = new NomiApiClient(apiKey);

var roster = new NomiRoster(client);
await roster.LoadAsync();

var adapter = new NomiProtocolAdapter();

var adapterRegistry = new AdapterRegistry();
await adapterRegistry.RegisterAsync(adapter, config: new NomiAdapterConfig
{
    Client = client,
    NomiId = string.Empty
});

var moduleRegistry = new UPSModuleRegistry();
moduleRegistry.Register(new UPSModuleInfo
{
    Name = "Nomi",
    Transport = "nomi-http",
    Endpoint = "https://api.nomi.ai"
});

var router = new ProtocolRouter(moduleRegistry, adapterRegistry);
var routing = new UPSRoutingService(router);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var host = new NomiBridgeHost(routing, roster, client, prefix);
Console.WriteLine("[AVA.Nomi.Bridge] Starting...");
await host.StartAsync(cts.Token);
Console.WriteLine("[AVA.Nomi.Bridge] Stopped.");