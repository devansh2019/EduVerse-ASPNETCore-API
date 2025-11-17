using ExaminationSystem.DTO.Accounts;
using ExaminationSystem.Mediators.Accounts;
using ExaminationSystem.Models;
using ExaminationSystem.Services.Accounts;
using ExaminationSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace ExaminationSystem.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAuthMediator _authMediator;
        private readonly UserManager<User> _userManager;
        public AccountController(IAuthMediator authMediator  , UserManager<User> usermanager )
        {
            _authMediator = authMediator;
            _userManager = usermanager;
        }

        [HttpPost]
        public async Task<ResultViewModel<AuthResponseDTO>> RegisterAsync(RegisterRequestDTO registerDTO)
        {
            var result = await _authMediator.Register(registerDTO);
            if (!result.IsAuthenticated)
            {
                return new ResultViewModel<AuthResponseDTO>
                {
                    IsSuccess = false,
                    Data = result
                };
            }

            SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);

            return new ResultViewModel<AuthResponseDTO>
            {
                IsSuccess = true,
                Data = result,
            };
        }

        [HttpPost]
        public async Task<ResultViewModel<AuthResponseDTO>> Login(LoginRequestDTO loginDTO)
        {
            var result = await _authMediator.Login(loginDTO);

            if (!result.IsAuthenticated)
            {
                return new ResultViewModel<AuthResponseDTO>
                {
                    IsSuccess = false,
                    Data = result
                };
            }

            if (!string.IsNullOrEmpty(result.RefreshToken))
                SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);

            return new ResultViewModel<AuthResponseDTO>
            {
                IsSuccess = true,
                Data = result,
            };
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string email, string code)
        {
            if (ModelState.IsValid)
            {
                var user = _userManager.FindByEmailAsync(email).Result;
                if (user != null)
                {
                    var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                    var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
                    if (result.Succeeded)
                    {
                        return Ok();
                    }
                }
            }
            return BadRequest("Error confirming your email.");
        }
        private void SetRefreshTokenInCookie(string refreshToken, DateTime expiration)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expiration.ToLocalTime(),
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.None,
            };

            Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);
        }

    }
}
