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
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);
            if (storedToken == null || storedToken.Expires < DateTime.UtcNow || storedToken.IsRevoked)
            {
                return Unauthorized("Invalid or expired refresh token");
            }

            var user = await _context.Users.FindAsync(storedToken.UserId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

			//generate new tokens
			var newAcessToken=GenerateJwtToken(user);
			var newRefreshToken = GenerateRefreshToken();

			//update refresh tokens
			storedToken.Token = newRefreshToken;
			storedToken.Expires = DateTime.UtcNow.AddDays(7);
			await _context.SaveChangesAsync();

			return Ok(
				new
				{
					AccessTolen = newAcessToken,
					RefreshToken = newRefreshToken
				}
			);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest("User already exists");
            }

            var user = new User
            {
                Username = request.Username,
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
			var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

			if (existingUser == null)
			{
				return Unauthorized(new { message = "user is not found" });
			}
			
			if (! BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash)) {
				return Unauthorized(new {message="Invalid password"});
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

			return Ok(
				new
				{
					AccessToken = accessToken,
					RefreshToken = refreshToken
				}
				);
		}


		private string GenerateRefreshToken()
		{
			var randonBytes = new byte[64];
			using(var rng=RandomNumberGenerator.Create())
			{
				rng.GetBytes(randonBytes);
			}
			return Convert.ToBase64String(randonBytes);
		}

		private string GenerateJwtToken(User user)
		{
			var jwtSettings = _configuration.GetSection("Jwt");
			var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Email,user.Email),
				new Claim(ClaimTypes.Role,user.Role),
				new Claim(ClaimTypes.NameIdentifier,user.Id.ToString())
			//NameIdentifier is a claim type used in ASP.NET Core Identity
			};
			var token = new JwtSecurityToken(
				issuer: jwtSettings["Issuer"],
				audience: jwtSettings["Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
				signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)

				);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t=>t.Token==request.RefreshToken);
			if(storedToken!=null)
			{
				storedToken.IsRevoked = true;
				await _context.SaveChangesAsync();
			}

			return Ok("logged out successfully");

		}

	}

	

}

