using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Veto;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddVeto();

await builder.Build().RunAsync();
