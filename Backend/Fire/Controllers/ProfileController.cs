using Microsoft.AspNetCore.Mvc;
using Yandes.DTOs;
using Yandes.Services;

namespace Yandes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IAuthService _authService;

        public ProfileController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public ActionResult<ApiResponse<object>> Get()
        {
            var email = HttpContext.Items["email"] as string ?? "";
            
            // Kullanıcı bilgilerini veritabanından al
            var userInfo = _authService.GetUserInfo(email);
            
            var data = new
            {
                email = userInfo.email,
                firstName = userInfo.firstName,
                lastName = userInfo.lastName,
                badges = new[] { "Yangın Gözlemci", "Doğa Dostu" },
                donations = new[] { new { date = DateTime.UtcNow.AddDays(-1), trees = 1 } }
            };
            return Ok(new ApiResponse<object> { Success = true, Data = data });
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateProfileRequest req)
        {
            var email = HttpContext.Items["email"] as string ?? "";
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Kullanıcı kimliği bulunamadı" });
            }

            var result = await _authService.UpdateProfileAsync(email, req);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPut("password")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var email = HttpContext.Items["email"] as string ?? "";
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Kullanıcı kimliği bulunamadı" });
            }

            var result = await _authService.ChangePasswordAsync(email, req);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}

