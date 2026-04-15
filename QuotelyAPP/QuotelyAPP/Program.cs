using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using QuotelyAPP;
using QuotelyAPP.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var proxyUrl = builder.Configuration["QuotableProxyUrl"]?.Trim();
var quoteApiBase = !string.IsNullOrEmpty(proxyUrl)
    ? proxyUrl.TrimEnd('/') + "/api/quotable/"
    : builder.HostEnvironment.IsDevelopment()
        ? "http://localhost:5266/api/quotable/"
        : new Uri(new Uri(builder.HostEnvironment.BaseAddress), "api/quotable/").ToString();

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(quoteApiBase) });
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<FavoritesService>();

await builder.Build().RunAsync();
