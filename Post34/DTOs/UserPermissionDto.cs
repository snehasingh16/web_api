using System.Text.Json.Serialization;

namespace Post34.DTOs;

public class UserPermissionDto
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("projects")]
    public List<int> Projects { get; set; } = new List<int>();
}