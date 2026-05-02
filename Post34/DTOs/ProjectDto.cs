using System.Text.Json.Serialization;

namespace Post34.DTOs;

public class ProjectDto
{
    [JsonPropertyName("name")]
    public string ProjectName { get; set; } = string.Empty;

    [JsonPropertyName("project_id")]
    public int ProjectId { get; set; }

    [JsonPropertyName("description")]
    public string ProjectDescription { get; set; } = string.Empty;
}
