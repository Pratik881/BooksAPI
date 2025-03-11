using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookStoreApi.Models;
using BookStoreApi.Data;
using Microsoft.EntityFrameworkCore;
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

		[HttpPost("register")]
		public async Task<IActionResult> Register(User user)
		{
			var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
			if (existingUser != null)
			{
				return BadRequest("User already exists");
			}
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
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
			var token = GenerateJwtToken(existingUser);
			return Ok(new { token });
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

	}
}
