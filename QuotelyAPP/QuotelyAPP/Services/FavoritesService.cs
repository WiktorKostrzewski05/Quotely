using QuotelyAPP.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace QuotelyAPP.Services
{
    public class FavoritesService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "quotely_favorites";

        public FavoritesService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<Quote>> GetFavoritesAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
                if (string.IsNullOrEmpty(json))
                    return new List<Quote>();

                return JsonSerializer.Deserialize<List<Quote>>(json) ?? new List<Quote>();
            }
            catch
            {
                return new List<Quote>();
            }
        }

        public async Task<bool> IsFavoriteAsync(Quote quote)
        {
            if (quote?._id == null)
                return false;

            var favorites = await GetFavoritesAsync();
            return favorites.Any(f => f._id == quote._id);
        }

        public async Task AddFavoriteAsync(Quote quote)
        {
            if (quote == null)
                return;

            var favorites = await GetFavoritesAsync();
            if (!favorites.Any(f => f._id == quote._id))
            {
                favorites.Add(quote);
                await SaveFavoritesAsync(favorites);
            }
        }

        public async Task RemoveFavoriteAsync(Quote quote)
        {
            if (quote?._id == null)
                return;

            var favorites = await GetFavoritesAsync();
            favorites.RemoveAll(f => f._id == quote._id);
            await SaveFavoritesAsync(favorites);
        }

        public async Task ToggleFavoriteAsync(Quote quote)
        {
            if (await IsFavoriteAsync(quote))
                await RemoveFavoriteAsync(quote);
            else
                await AddFavoriteAsync(quote);
        }

        private async Task SaveFavoritesAsync(List<Quote> favorites)
        {
            var json = JsonSerializer.Serialize(favorites);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
    }
}
