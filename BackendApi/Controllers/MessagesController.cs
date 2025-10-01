using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AiClient _ai;

        public MessagesController(AppDbContext db, AiClient ai)
        {
            _db = db;
            _ai = ai;
        }

        // JSON binder case-insensitive olduğu için "alias"/"text" olarak da gelir.
        public record CreateReq(string alias, string text);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReq req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.text))
                return BadRequest("text required");

            // >>> HF Space'e çağrı
            var (label, score) = await _ai.AnalyzeAsync(req.text); // <- AiClient.AnalyzeAsync
            // Eğer AiClient'ta PredictAsync yazdıysan yukarıyı: await _ai.PredictAsync(req.text);
            // <<<

            var msg = new Message
            {
                UserAlias = string.IsNullOrWhiteSpace(req.alias) ? "anon" : req.alias,
                Text = req.text.Trim(),
                SentimentLabel = label,
                SentimentScore = score,
                CreatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            // Frontend'lerin beklediği şekle yakın bir cevap dönelim
            return Created($"/api/messages/{msg.Id}", new
            {
                id = msg.Id,
                alias = msg.UserAlias, // web & mobil için kısa ad
                text = msg.Text,
                sentiment = new { label = msg.SentimentLabel, score = msg.SentimentScore },
                createdAt = msg.CreatedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? alias, [FromQuery] int limit = 50)
        {
            var q = _db.Messages.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(alias))
                q = q.Where(m => m.UserAlias == alias);

            var data = await q
                .OrderByDescending(m => m.Id)
                .Take(limit)
                .Select(m => new
                {
                    id = m.Id,
                    alias = m.UserAlias,
                    text = m.Text,
                    sentiment = new { label = m.SentimentLabel, score = m.SentimentScore },
                    // Eski frontend alan isimleriyle uyum için aşağıdakileri de istersek bırakabiliriz:
                    sentimentLabel = m.SentimentLabel,
                    sentimentScore = m.SentimentScore,
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
