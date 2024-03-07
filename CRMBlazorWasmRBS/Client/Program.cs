using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

using CRMBlazorWasmRBS.Client;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRadzenComponents();
builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<CRMBlazorWasmRBS.Client.RadzenCRMService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddHttpClient("CRMBlazorWasmRBS.Server", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("CRMBlazorWasmRBS.Server"));
builder.Services.AddScoped<CRMBlazorWasmRBS.Client.SecurityService>();
builder.Services.AddScoped<AuthenticationStateProvider, CRMBlazorWasmRBS.Client.ApplicationAuthenticationStateProvider>();

var host = builder.Build();
await host.RunAsync();