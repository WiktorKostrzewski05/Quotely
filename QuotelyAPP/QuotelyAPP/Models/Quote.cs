namespace QuotelyAPP.Models
{
    public class Quote
    {
        public string? _id { get; set; }
        public string? content { get; set; }
        public string? author { get; set; }
        public List<string>? tags { get; set; }
    }
}
