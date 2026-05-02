using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json;

namespace Post34.Models;

[BsonIgnoreExtraElements]
public class ApiDefinition
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("parent_project_id")]
    public string parent_project_id { get; set; } = string.Empty;

    [BsonElement("api_name")]
    public string api_name { get; set; } = string.Empty;

    [BsonElement("method")]
    public string method { get; set; } = string.Empty; // GET, POST, PUT, DELETE

    [BsonElement("url")]
    public string url { get; set; } = string.Empty;

    [BsonElement("description")]
    public string description { get; set; } = string.Empty;

    [BsonElement("request_body")]
    public BsonDocument? request_body { get; set; } // Store as BSON document
}