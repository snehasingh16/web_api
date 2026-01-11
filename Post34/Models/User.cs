using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Post34.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string username { get; set; } = string.Empty;

    public string passwordHash { get; set; } = string.Empty;

    public string? role { get; set; }
}
