using FileDivider.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FileDivider.Api.Data
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;

        public MongoContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<PdfTemplate> PdfTemplates => _database.GetCollection<PdfTemplate>("PdfTemplates");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}
