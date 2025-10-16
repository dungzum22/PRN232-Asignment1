using Microsoft.IdentityModel.Tokens;
using ShopNew.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MongoDB.Driver;

namespace ShopNew.Services
{
    public class AuthService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IConfiguration _configuration;
        private readonly MongoSequenceService _sequenceService;

        public AuthService(IMongoDatabase database, IConfiguration configuration, MongoSequenceService sequenceService)
        {
            _users = database.GetCollection<User>("users");
            _configuration = configuration;
            _sequenceService = sequenceService;
        }

        public async Task<User?> RegisterAsync(string email, string password)
        {
            var existingUser = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null) return null;

            // Check if this is the first user (make them admin)
            var userCount = await _users.CountDocumentsAsync(Builders<User>.Filter.Empty);
            var isFirstUser = userCount == 0;

            var user = new User
            {
                Id = await _sequenceService.GetNextSequenceAsync("users"),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = isFirstUser ? UserRoles.Admin : UserRoles.User,
                CreatedAt = DateTime.UtcNow
            };

            await _users.InsertOneAsync(user);
            return user;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
