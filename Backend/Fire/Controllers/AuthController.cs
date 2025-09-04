using Microsoft.AspNetCore.Mvc;
using Yandes.DTOs;
using Yandes.Services;
using System.Data.Odbc;

namespace Yandes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        // Ping ve dbinfo minimal endpoint'ler üzerinden yayınlanıyor (Program.cs)

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest req)
        {
            var res = await _auth.RegisterAsync(req);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest req)
        {
            var res = await _auth.LoginAsync(req);
            if (!res.Success) return Unauthorized(res);
            return Ok(res);
        }
    }
}

