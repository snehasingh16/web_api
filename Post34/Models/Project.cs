using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Post34.DTOs;

namespace Post34.Models;

[BsonIgnoreExtraElements]
public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }  // ✅ Primary key for MongoDB

    [BsonElement("project_id")]
    public int project_id { get; set; } = 0;

    [BsonElement("name")]
    public string project_name { get; set; } = string.Empty;

    [BsonElement("proj_description")]
    public string proj_description { get; set; } = string.Empty;

    [BsonElement("proj_perm")]
    public string proj_permission { get; set; } = string.Empty;
}
