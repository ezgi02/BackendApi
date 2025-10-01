using System.Net.Http.Headers;
using System.Text.Json;

namespace BackendApi.Services
{
    public class AiClient
    {
        private readonly HttpClient _http;
        private readonly string? _url;

        public AiClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(30);
            _url = Environment.GetEnvironmentVariable("AI_URL") ?? cfg["AI_URL"];
        }

        private static (string label, double score) Fallback(string text)
        {
            var t = (text ?? "").ToLower();
            if (t.Contains("harika") || t.Contains("iyi")) return ("positive", 0.90);
            if (t.Contains("üzgün") || t.Contains("kötü")) return ("negative", 0.85);
            return ("neutral", 0.50);
        }

        private static string Normalize(string? lab)
        {
            var s = (lab ?? "neutral").ToLower();
            if (s.StartsWith("label_"))
            {
                var map = new[] { "negative", "neutral", "positive" };
                if (int.TryParse(s.Split('_')[1], out var i) && i >= 0 && i < map.Length)
                    return map[i];
                return "neutral";
            }
            return s;
        }

        private sealed class GradioItem { public string? label { get; set; } public double? score { get; set; } }
        private sealed class GradioResp { public List<GradioItem>? data { get; set; } }

        public async Task<(string label, double score)> AnalyzeAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_url))
                return Fallback(text);

            try
            {
                var resp = await _http.PostAsJsonAsync(_url.TrimEnd('/'), new { data = new[] { text } });
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadFromJsonAsync<GradioResp>();
                var item = json?.data?.FirstOrDefault();
                if (item == null) return Fallback(text);
                return (Normalize(item.label), item.score ?? 0.0);
            }
            catch
            {
                return Fallback(text);
            }
        }
    }
    }

