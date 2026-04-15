using System.Net.Http.Json;
using QuotelyAPP.Models;

namespace QuotelyAPP.Services
{
    public class QuoteService
    {
        private readonly HttpClient _http;

        public QuoteService(HttpClient http)
        {
            _http = http;
        }

        public async Task<Quote?> GetRandomQuote(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<Quote>("random", cancellationToken);
            }
            catch
            {
                return null;
            }
        }
        
        public async Task<QuoteSearchResult?> SearchQuotes(string query, int page = 1, int limit = 10)
        {
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"quotes?query={encodedQuery}&page={page}&limit={limit}";
            return await _http.GetFromJsonAsync<QuoteSearchResult>(url);
        }

        public async Task<AuthorSearchResult?> SearchAuthors(string query, int page = 1, int limit = 100)
        {
            string encodedQuery = Uri.EscapeDataString(query);
            return await _http.GetFromJsonAsync<AuthorSearchResult>(
                $"authors?query={encodedQuery}&page={page}&limit={limit}"
            );
        }

        public async Task<List<Tag>?> GetTags()
        {
            return await _http.GetFromJsonAsync<List<Tag>>("tags");
        }
    }
}
