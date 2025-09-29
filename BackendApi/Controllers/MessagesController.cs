using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AiClient _ai;

        public MessagesController(AppDbContext db, AiClient ai)
        {
            _db = db; _ai = ai;
        }
        //kayıtlı bu tipler, her istek için enjekte edilir.
        public record CreateReq(string alias, string text);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReq req)
        {
            if (string.IsNullOrWhiteSpace(req?.text))
                return BadRequest("text required");

            var ai = await _ai.PredictAsync(req.text);//AI 'dan label+score al
            var msg = new Message  //Db entity 'si
            {
                UserAlias = req.alias,
                Text = req.text,
                SentimentLabel = ai.label,
                SentimentScore = ai.score
            };
            _db.Messages.Add(msg); //Ef takip listesine ekle
            await _db.SaveChangesAsync();//Insert

            return Created($"/api/messages/{msg.Id}", new //201+body
            {
                id = msg.Id,
                alias = msg.UserAlias,
                text = msg.Text,
                sentiment = new { label = msg.SentimentLabel, score = msg.SentimentScore },
                createdAt = msg.CreatedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? alias, [FromQuery] int limit = 50)
        {
            var q = _db.Messages.AsQueryable();        //sorgu inşa
            if (!string.IsNullOrWhiteSpace(alias))
                q = q.Where(m => m.UserAlias == alias); //alias'a göre filtre

            var data = await q.OrderByDescending(m => m.Id).Take(limit).ToListAsync(); // en yeniler önce 'async'çalış 200+liste
            return Ok(data);
        }

    }
}
