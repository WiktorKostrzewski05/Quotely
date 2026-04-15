namespace QuotelyAPP.Models
{
    public class QuoteSearchResult
    {
        public int count { get; set; }
        public int totalCount { get; set; }
        public int page { get; set; }
        public int totalPages { get; set; }
        public List<Quote>? results { get; set; }
    }
}