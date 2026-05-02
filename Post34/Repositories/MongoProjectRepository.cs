using MongoDB.Driver;
using Post34.Helpers;
using Post34.Models;

namespace Post34.Repositories;

public class MongoProjectRepository : IProjectRepository
{
    private readonly IMongoCollection<Project> _projects;

    public MongoProjectRepository(MongoSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("MongoDB connection string is not configured in appsettings.json.");

        var client = new MongoClient(settings.ConnectionString);
        var db = client.GetDatabase(settings.Database);
        _projects = db.GetCollection<Project>("ProjectList");
    }

    public async Task<List<Project>> GetAllAsync()
    {
        return await _projects.Find(_ => true).ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(string objectId)
    {
        try
        {
            return await _projects.Find(p => p.Id == objectId).FirstOrDefaultAsync();
        }
        catch
        {
            return null;
        }
    }
}