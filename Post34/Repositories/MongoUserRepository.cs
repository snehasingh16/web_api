using MongoDB.Driver;
using Post34.Helpers;
using Post34.Models;

namespace Post34.Repositories;

public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(MongoSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("MongoDB connection string is not configured in appsettings.json.");

        var client = new MongoClient(settings.ConnectionString);
        var db = client.GetDatabase(settings.Database);
        _users = db.GetCollection<User>("Users");

        // Best-effort: create a unique index on username. Ignore failures (legacy data may cause issues).
        try
        {
            var idx = new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.username), new CreateIndexOptions { Unique = true });
            _users.Indexes.CreateOne(idx);
        }
        catch
        {
            // ignore - index creation is best-effort in minimal implementation
        }
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var filter = Builders<User>.Filter.Eq(u => u.username, username);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task AddAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }
}
