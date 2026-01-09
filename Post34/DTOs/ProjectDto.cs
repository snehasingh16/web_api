using System.Text.Json.Serialization;

namespace Post34.DTOs;

public class ProjectDto
{
    [JsonPropertyName("project_name")]
    public string ProjectName { get; set; } = string.Empty;

    [JsonPropertyName("project_id")]
    public int ProjectId { get; set; }

    // using the exact key the user requested
    [JsonPropertyName("permision")]
    public bool Permission { get; set; }
}
