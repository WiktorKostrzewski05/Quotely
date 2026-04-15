using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/quotable")]
public class QuotableProxyController : ControllerBase
{
    private const string QuotableBase = "https://api.quotable.io/";
    private const string ZenQuotesRandomUrl = "https://zenquotes.io/api/random";
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri(QuotableBase),
        DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
    };

    private readonly ILogger<QuotableProxyController> _logger;
    private static readonly (string Content, string Author)[] LocalFallbackQuotes =
    [
        ("Success is not final, failure is not fatal: it is the courage to continue that counts.", "Winston Churchill"),
        ("The best way to predict the future is to create it.", "Peter Drucker"),
        ("Small steps every day lead to big results.", "Unknown"),
        ("Do what you can, with what you have, where you are.", "Theodore Roosevelt")
    ];

    public QuotableProxyController(ILogger<QuotableProxyController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{*path}")]
    public async Task<IActionResult> Proxy(string? path, CancellationToken cancellationToken)
    {
        var relativePath = (path ?? "").TrimStart('/');
        var query = Request.QueryString.Value ?? "";
        var url = string.IsNullOrEmpty(relativePath) ? query.TrimStart('?') : relativePath + query;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(3));

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await HttpClient.GetAsync(url, linkedCts.Token);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsByteArrayAsync(linkedCts.Token);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
                return File(content, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Quotable proxy attempt {Attempt} failed for {Url}", attempt, url);
                if (attempt == 3)
                {
                    _logger.LogError(ex, "Quotable proxy failed after 3 attempts for {Url}", url);
                    if (string.Equals(relativePath, "random", StringComparison.OrdinalIgnoreCase))
                    {
                        var fallback = await TryGetFallbackRandomQuote(linkedCts.Token) ?? GetLocalFallbackRandomQuote();
                        if (!string.IsNullOrEmpty(fallback))
                        {
                            return Content(fallback, "application/json");
                        }
                    }
                    return StatusCode(502, "Quote service temporarily unavailable. Please try again.");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), linkedCts.Token);
            }
        }

        if (string.Equals(relativePath, "random", StringComparison.OrdinalIgnoreCase))
        {
            var fallback = GetLocalFallbackRandomQuote();
            return Content(fallback, "application/json");
        }

        return StatusCode(502, "Quote service temporarily unavailable.");
    }

    private async Task<string?> TryGetFallbackRandomQuote(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await HttpClient.GetAsync(ZenQuotesRandomUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
            {
                return null;
            }

            var first = document.RootElement[0];
            var content = first.TryGetProperty("q", out var qValue) ? qValue.GetString() : null;
            var author = first.TryGetProperty("a", out var aValue) ? aValue.GetString() : null;

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var payload = new
            {
                _id = Guid.NewGuid().ToString("N"),
                content,
                author = string.IsNullOrWhiteSpace(author) ? "Unknown" : author,
                tags = Array.Empty<string>()
            };

            return JsonSerializer.Serialize(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback random quote fetch failed");
            return null;
        }
    }

    private static string GetLocalFallbackRandomQuote()
    {
        var selected = LocalFallbackQuotes[Random.Shared.Next(LocalFallbackQuotes.Length)];
        var payload = new
        {
            _id = Guid.NewGuid().ToString("N"),
            content = selected.Content,
            author = selected.Author,
            tags = Array.Empty<string>()
        };
        return JsonSerializer.Serialize(payload);
    }
}
