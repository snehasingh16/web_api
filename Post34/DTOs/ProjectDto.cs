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

    [JsonPropertyName("used_services_list")]
    public List<ServiceItem> used_services_list { get; set; } = new();
}


public class ServiceItem
{
    public string service_name { get; set; }
    public int service_id { get; set; }
}