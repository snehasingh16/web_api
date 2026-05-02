using MongoDB.Driver;
using Post34.Helpers;
using Post34.Models;

namespace Post34.Repositories;

public class MongoApiDefinitionRepository : IApiDefinitionRepository
{
    private readonly IMongoCollection<ApiDefinition> _apiDefinitions;

    public MongoApiDefinitionRepository(MongoSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("MongoDB connection string is not configured in appsettings.json.");

        var client = new MongoClient(settings.ConnectionString);
        var db = client.GetDatabase(settings.Database);
        _apiDefinitions = db.GetCollection<ApiDefinition>("ServicesList");
    }

    public async Task<List<ApiDefinition>> GetAllAsync()
    {
        return await _apiDefinitions.Find(_ => true).ToListAsync();
    }

    public async Task<List<ApiDefinition>> GetByParentProjectIdAsync(string parentProjectId)
    {
        return await _apiDefinitions.Find(api => api.parent_project_id == parentProjectId).ToListAsync();
    }

    public async Task<ApiDefinition?> GetByIdAsync(string objectId)
    {
        try
        {
            return await _apiDefinitions.Find(api => api.Id == objectId).FirstOrDefaultAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task CreateAsync(ApiDefinition apiDefinition)
    {
        await _apiDefinitions.InsertOneAsync(apiDefinition);
    }

    public async Task UpdateAsync(string objectId, ApiDefinition apiDefinition)
    {
        var filter = Builders<ApiDefinition>.Filter.Eq(api => api.Id, objectId);
        await _apiDefinitions.ReplaceOneAsync(filter, apiDefinition);
    }

    public async Task DeleteAsync(string objectId)
    {
        var filter = Builders<ApiDefinition>.Filter.Eq(api => api.Id, objectId);
        await _apiDefinitions.DeleteOneAsync(filter);
    }
}