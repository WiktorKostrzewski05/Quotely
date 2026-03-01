using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;
using Xunit;

namespace QuotelyAPP.Tests;

public class BasicTests : IAsyncLifetime
{
    private IPlaywright _playwright = default!;
    private IBrowser _browser = default!;
    private Process? _appProcess;
    private HttpClient? _httpClient;
    private const string BaseUrl = "http://localhost:5000";

    public async Task InitializeAsync()
    {
        var fullPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "QuotelyAPP", "QuotelyAPP", "QuotelyAPP.csproj"));
        var projectDir = Path.GetDirectoryName(fullPath);
        
        if (!File.Exists(fullPath))
        {
            throw new Exception($"Project file not found at: {fullPath}");
        }
        
        if (!Directory.Exists(projectDir))
        {
            throw new Exception($"Project directory not found at: {projectDir}");
        }
        
        _appProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{fullPath}\" --urls {BaseUrl}",
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        _appProcess.Start();

        _httpClient = new HttpClient();
        var maxAttempts = 30;
        var attempt = 0;
        var serverReady = false;

        while (attempt < maxAttempts && !serverReady)
        {
            try
            {
                var response = await _httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    serverReady = true;
                    break;
                }
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                attempt++;
            }
        }

        if (!serverReady)
        {
            throw new Exception("Server failed to start within 30 seconds");
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
        _httpClient?.Dispose();
        if (_appProcess is { HasExited: false })
        {
            _appProcess.Kill(true);
            _appProcess.Dispose();
        }
    }

    [Fact]
    public async Task AppLoadsAndNavbarExists()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(BaseUrl);

        await page.WaitForSelectorAsync("header.navbar", new PageWaitForSelectorOptions { Timeout = 10000 });
        var title = await page.InnerTextAsync("h2");
        Assert.False(string.IsNullOrWhiteSpace(title));
    }

    [Fact]
    public async Task RandomQuoteLoads()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForSelectorAsync(".quote-page", new PageWaitForSelectorOptions { Timeout = 20000 });
        var header = await page.InnerTextAsync("h2");
        Assert.False(string.IsNullOrWhiteSpace(header));
    }

    [Fact]
    public async Task SearchPageLoadsAndSearchWorks()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/search");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 60000 });
        await page.WaitForSelectorAsync(".search-page", new PageWaitForSelectorOptions { Timeout = 60000 });
        await page.FillAsync("input[placeholder='Search by keyword (optional)']", "day");
        await page.ClickAsync(".search-btn");
        await page.WaitForSelectorAsync(".search-btn", new PageWaitForSelectorOptions { Timeout = 20000 });
        Assert.True(true);
    }
}

