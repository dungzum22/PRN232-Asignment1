using MongoDB.Bson;
using MongoDB.Driver;

namespace ShopNew.Services
{
    public class MongoSequenceService
    {
        private readonly IMongoCollection<BsonDocument> _counters;

        public MongoSequenceService(IMongoDatabase database)
        {
            _counters = database.GetCollection<BsonDocument>("counters");
        }

        public async Task<int> GetNextSequenceAsync(string name)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", name);
            var update = Builders<BsonDocument>.Update.Inc("seq", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var doc = await _counters.FindOneAndUpdateAsync(filter, update, options);
            return doc.GetValue("seq").AsInt32;
        }
    }
}


