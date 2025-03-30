using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookStoreApi.Models;
using BookStoreApi.Data;
using Microsoft.EntityFrameworkCore;
using BookStoreApi.DTO;
using System.Security.Cryptography;
using Azure.Core;
namespace BookStoreApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{

		private readonly BookStoreContext _context;
		private readonly IConfiguration _configuration;

		public AuthController(BookStoreContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		[HttpPost("refresh")]
		public async Task<IActionResult> Refresh()
		{
			if (!Request.Cookies.TryGetValue("refreshToken", out var refreshTOken))
			{
				return Unauthorized("No refresh token found");
			}
			var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshTOken);
			if (storedToken == null || storedToken.Expires < DateTime.UtcNow || storedToken.IsRevoked)
			{
				return Unauthorized("Invalid or expired refresh token");
			}

			var user = await _context.Users.FindAsync(storedToken.UserId);
			if (user == null)
			{
				return Unauthorized("user not found");
			}

			//Generae new tokens
			var newAccessToken = GenerateJwtToken(user);
			var newRefreshToken = GenerateRefreshToken();
			storedToken.IsRevoked = true;
			var newTokenEntry = new RefreshToken
			{
				Token = newRefreshToken,
				Expires = DateTime.UtcNow.AddDays(7),
				UserId = user.Id,
				IsRevoked = false
			};
			_context.RefreshTokens.Add(newTokenEntry);
			await _context.SaveChangesAsync();

			var cookieOptions = new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Strict,
				Expires = DateTime.UtcNow.AddDays(7)
			};

			Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);
			return Ok(
				new
				{
					AccessToken = newAccessToken
				}
			);
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}
			var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
			if (existingUser != null)
			{
				return BadRequest("User already exists");
			}

			var user = new User
			{
				Email = request.Email,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
				Role = "User" // Default role
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();
			return Ok("User registered successfully");
		}


		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			if(!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}
			var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

			if (existingUser == null)
			{
				return Unauthorized(new { message = "user is not found" });
			}

			if (!BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash))
			{
				return Unauthorized(new { message = "Invalid password" });
			}
			var accessToken = GenerateJwtToken(existingUser);
			var refreshToken = GenerateRefreshToken();

			var newRefreshToken = new RefreshToken
			{
				Token = refreshToken,
				UserId = existingUser.Id,
				Expires = DateTime.UtcNow.AddDays(7),
				IsRevoked = false
			};

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var cookieOptions = new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Strict,
				Expires = DateTime.UtcNow.AddDays(7),
			};

			Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

			return Ok(
				new
				{
					AccessToken = accessToken
				}
			);
		}


		private string GenerateRefreshToken()
		{
			var randonBytes = new byte[64];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(randonBytes);
			}
			return Convert.ToBase64String(randonBytes);
		}
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            
            var keyString = jwtSettings["Key"] ??
                throw new InvalidOperationException("JWT Key is not configured");
            var issuer = jwtSettings["Issuer"] ??
                throw new InvalidOperationException("JWT Issuer is not configured");
            var audience = jwtSettings["Audience"] ??
                throw new InvalidOperationException("JWT Audience is not configured");

            if (!double.TryParse(jwtSettings["ExpiryMinutes"], out double expiryMinutes))
                throw new InvalidOperationException("JWT ExpiryMinutes is not properly configured");

            
            if (Encoding.UTF8.GetBytes(keyString).Length < 32)
                throw new InvalidOperationException("JWT Key must be at least 32 bytes (256 bits)");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("logout")]
		public async Task<IActionResult> Logout()
		{
			if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
			{
				return Unauthorized("No refresh token found");
			}

            Console.WriteLine($"Refresh Token from Cookie: {refreshToken}");

            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
			if (storedToken != null)
			{
				_context.RefreshTokens.Remove(storedToken);
				await _context.SaveChangesAsync();
			}
			Response.Cookies.Delete("refreshToken");

			return Ok("logged out successfully");

		}

	}



}

