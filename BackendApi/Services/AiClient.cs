using System.Net.Http.Headers;
using System.Text.Json;

namespace BackendApi.Services
{
    public class AiClient
    {
        // public record AiResp(string label, double score);

        // Şimdilik gerçek modele bağlanmadan sahte yanıt döndürüyoruz:
        /* public Task<AiResp> PredictAsync(string text)
         {
             // Basit bir kural: "iyi" geçerse positive, "kötü/üzgün" geçerse negative, değilse neutral
             var t = text?.ToLowerInvariant() ?? "";
             if (t.Contains("iyi") || t.Contains("harika") || t.Contains("mutlu"))
                 return Task.FromResult(new AiResp("positive", 0.90));
             if (t.Contains("kötü") || t.Contains("üzgün") || t.Contains("moral"))
                 return Task.FromResult(new AiResp("negative", 0.85));
             return Task.FromResult(new AiResp("neutral", 0.50));
         }*/

        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public AiClient(HttpClient http, IConfiguration cfg)
        {
            _http = http; _cfg = cfg;
        }
        public record AiResp(string label, double score);
        public record HFScore(string label, double score);

        public async Task<AiResp> PredictAsync(string text)
        {
            // 1) HF Inference API dene
            var token = _cfg["HF_API_TOKEN"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var url = "https://api-inference.huggingface.co/models/cardiffnlp/twitter-xlm-roberta-base-sentiment?wait_for_model=true";
                    using var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Inference API beklediği format:
                    var payload = new { inputs = text ?? "" };
                    req.Content = JsonContent.Create(payload);

                    var resp = await _http.SendAsync(req);
                    resp.EnsureSuccessStatusCode();

                    // Dönüş: List<List<HFScore>> gibi olur (tek giriş için 1 liste)
                    var arr = await resp.Content.ReadFromJsonAsync<List<List<HFScore>>>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var first = arr?.FirstOrDefault();
                    if (first is not null && first.Count > 0)
                    {
                        // En yüksek skorlu etiketi al
                        var best = first.OrderByDescending(s => s.score).First();
                        // Bu model label'ları zaten: negative / neutral / positive
                        return new AiResp(best.label, best.score);
                    }
                }
                catch
                {
                    // sessizce mock'a düş
                }
            }
            var t = (text ?? "").ToLowerInvariant();
            if (t.Contains("iyi") || t.Contains("harika") || t.Contains("mutlu"))
                return new AiResp("positive", 0.90);
            if (t.Contains("kötü") || t.Contains("üzgün") || t.Contains("moral"))
                return new AiResp("negative", 0.85);
            return new AiResp("neutral", 0.50);
        }

    }
    }

