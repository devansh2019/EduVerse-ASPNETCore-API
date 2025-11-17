using Azure.Core;
using Exam.Service;
using ExaminationSystem.DTO.Accounts;
using ExaminationSystem.Helpers;
using ExaminationSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExaminationSystem.Services.Accounts
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;

        public AuthService(
            UserManager<User> userManager, 
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration,
            IHttpContextAccessor contextaccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _contextAccessor = contextaccessor;
        }

        public async Task<AuthResponseDTO> Login(LoginRequestDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user is null || !await _userManager.CheckPasswordAsync(user, loginDTO.Password))
            {
                return new AuthResponseDTO() 
                { 
                    Message = "Email or Password is incorrect!" 
                };
            }

            // Check if email is confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return new AuthResponseDTO()
                {
                    Message = "Email is not confirmed. Please check your email."
                };
            }

            var token = await CreatJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            if (user.RefreshTokens.Any(t => t.IsActive))
            {
                var activeRefreshToken = user.RefreshTokens.FirstOrDefault(t => t.IsActive);
                return new AuthResponseDTO
                {
                    IsAuthenticated = true,
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    ExpiresOn = token.ValidTo,
                    Message = "Login Successed",
                    Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                    RefreshToken = activeRefreshToken.Token,
                    RefreshTokenExpiration = activeRefreshToken.ExpiredOn
                };
            }

            user.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(user);

            return new AuthResponseDTO
            {
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresOn = token.ValidTo,
                Message = "Login Successed",
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiredOn
            };
        }

        public async Task<AuthResponseDTO> Register(RegisterRequestDTO registerDTO)
        {
            if (await _userManager.FindByNameAsync(registerDTO.UserName) is not null)
            {
                return new AuthResponseDTO()
                {
                    Message = "Username is already registered!"
                };
            }

            if (await _userManager.FindByEmailAsync(registerDTO.Email) is not null)
            {
                return new AuthResponseDTO() 
                { 
                    Message = "Email is already registered!" 
                };
            }

            var user = registerDTO.MapOne<User>();
            
            // Create user FIRST
            var result = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!result.Succeeded)
            {
                var errors = string.Empty;
                foreach (var error in result.Errors)
                {
                    errors += $"{error.Description},";
                }
                return new AuthResponseDTO() 
                { 
                    Message = errors
                };
            }

            // Assign role
            if (registerDTO.Type == "Instructor")
                await _userManager.AddToRoleAsync(user, "Instructor");
            else if (registerDTO.Type == "Student")
                await _userManager.AddToRoleAsync(user, "Student");

            // Generate confirmation token and send email
            var scheme = _contextAccessor.HttpContext.Request.Scheme;
            var host = _contextAccessor.HttpContext.Request.Host.Value;

            var tokenConfirmation = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(tokenConfirmation));
            var confirmationLink = $"{scheme}://{host}/account/ConfirmEmail?email={user.Email}&code={code}";

            EmailService emailService = new EmailService(_configuration);
            await emailService.Execute(user.Email, confirmationLink);

            // Return success WITHOUT authentication tokens
            return new AuthResponseDTO
            {
                IsAuthenticated = false,
                Message = "Registration successful. Please check your email to confirm your account."
            };
        }

        private async Task<JwtSecurityToken> CreatJwtToken(User user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();

            foreach (var role in roles)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
                 

            }
            .Union(userClaims)
            .Union(roleClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));

            var signInCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            double.TryParse(_configuration["JWT:DurationInMintues"], out double DurationInMintues);

            var expiryTime = DateTime.UtcNow.AddMinutes(DurationInMintues);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudiance"],
                claims: claims,
                expires: expiryTime,
                signingCredentials: signInCredentials
            );
            return token;
        }

        private RefreshToken GenerateRefreshToken()
        {
            var RandomNumber = new Byte[32];
            using var generator = new RNGCryptoServiceProvider();
            generator.GetBytes(RandomNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumber),
                ExpiredOn = DateTime.UtcNow.AddDays(10),
                CreatedOn = DateTime.UtcNow,
            };
        }

    }
}
