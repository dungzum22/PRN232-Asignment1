using ShopNew.Models;
using MongoDB.Driver;

namespace ShopNew.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("users");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _users.Find(Builders<User>.Filter.Empty).ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            var update = Builders<User>.Update.Set(u => u.Role, newRole);
            var result = await _users.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var result = await _users.DeleteOneAsync(u => u.Id == userId);
            return result.DeletedCount > 0;
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            var count = await _users.CountDocumentsAsync(Builders<User>.Filter.Empty);
            return (int)count;
        }
    }
}


