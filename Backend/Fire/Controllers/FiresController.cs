using Microsoft.AspNetCore.Mvc;
using Yandes.DTOs;
using Yandes.Services;

namespace Yandes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FiresController : ControllerBase
    {
        private readonly IFireService _fireService;

        public FiresController(IFireService fireService)
        {
            _fireService = fireService;
        }

        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponse<IEnumerable<FireHotspotDto>>>> GetRecent()
        {
            var result = await _fireService.GetRecentHotspotsAsync();
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("nearby")]
        public async Task<ActionResult<ApiResponse<IEnumerable<FireHotspotDto>>>> GetNearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusKm = 50)
        {
            var result = await _fireService.GetNearbyHotspotsAsync(lat, lng, radiusKm);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}

