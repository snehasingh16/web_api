using Post34.Models;

namespace Post34.Repositories;

public interface IApiDefinitionRepository
{
    Task<List<ApiDefinition>> GetAllAsync();
    Task<List<ApiDefinition>> GetByParentProjectIdAsync(string parentProjectId);
    Task<ApiDefinition?> GetByIdAsync(string objectId);
    Task CreateAsync(ApiDefinition apiDefinition);
    Task UpdateAsync(string objectId, ApiDefinition apiDefinition);
    Task DeleteAsync(string objectId);
}