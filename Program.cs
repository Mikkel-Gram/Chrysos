using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Chrysos;
using Chrysos.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<BrowserInterop>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<LibraryService>();
builder.Services.AddSingleton<ProgramBuilder>();
builder.Services.AddSingleton<ProgramLibraryService>();
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<ProgramGenerator>();
builder.Services.AddSingleton<SessionState>();

await builder.Build().RunAsync();
