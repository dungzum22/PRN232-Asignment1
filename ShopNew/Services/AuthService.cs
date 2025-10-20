using Microsoft.IdentityModel.Tokens;
using ShopNew.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ShopNew.Data;
using Microsoft.EntityFrameworkCore;

namespace ShopNew.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _jwtKey = GetJwtSetting("Key");
            _jwtIssuer = GetJwtSetting("Issuer", "ShopNew");
            _jwtAudience = GetJwtSetting("Audience", "ShopNewUsers");
        }

        public async Task<User?> RegisterAsync(string email, string password)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null) return null;

            // Check if this is the first user (make them admin)
            var userCount = await _context.Users.CountAsync();
            var isFirstUser = userCount == 0;

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = isFirstUser ? UserRoles.Admin : UserRoles.User,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GetJwtSetting(string name, string? defaultValue = null)
        {
            var value = _configuration[$"Jwt:{name}"] ??
                        _configuration[$"JWT_{name.ToUpperInvariant()}"] ??
                        _configuration[$"Jwt__{name}"] ??
                        defaultValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"JWT {name} is not configured. Please set Jwt:{name} in configuration or provide JWT_{name.ToUpperInvariant()} environment variable.");
            }

            return value;
        }
    }
}
