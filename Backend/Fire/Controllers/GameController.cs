using Microsoft.AspNetCore.Mvc;
using Yandes.DTOs;

namespace Yandes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        [HttpGet("state")]
        public ActionResult<ApiResponse<object>> GetState()
        {
            var username = HttpContext.Items["username"] as string ?? "";
            var data = new
            {
                username,
                points = 10,
                plant = "Fidan",
                tasks = new[]
                {
                    new { key = "check_fire", title = "En yakın yangını kontrol et", done = false },
                    new { key = "share_risk", title = "Risk uyarısını paylaş", done = false },
                    new { key = "complete_one", title = "1 görev tamamla", done = false }
                }
            };
            return Ok(new ApiResponse<object> { Success = true, Data = data });
        }

        [HttpPost("complete")]
        public ActionResult<ApiResponse<object>> Complete([FromBody] Dictionary<string, string> body)
        {
            var key = body.TryGetValue("key", out var v) ? v : string.Empty;
            return Ok(new ApiResponse<object> { Success = true, Message = $"Görev tamamlandı: {key}", Data = new { points = 20 } });
        }
    }
}

