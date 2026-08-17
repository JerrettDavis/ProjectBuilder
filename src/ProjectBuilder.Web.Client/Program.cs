using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProjectBuilder.Web.Client.DraftPersistence;
using ProjectBuilder.Web.Client.Guidance;
using ProjectBuilder.Web.Client.Workshop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserDraftStore>();
builder.Services.AddScoped<GuidanceSessionStore>();
builder.Services.AddScoped<WorkshopSessionStore>();

await builder.Build().RunAsync();
