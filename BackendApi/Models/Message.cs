namespace BackendApi.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string UserAlias { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? SentimentLabel { get; set; }
        public double? SentimentScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
