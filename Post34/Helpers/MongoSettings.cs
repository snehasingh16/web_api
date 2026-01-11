namespace Post34.Helpers;

public class MongoSettings
{
    // Leave blank by default to avoid committing credentials.
    // Set via appsettings.json, environment variables or `dotnet user-secrets`.
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}
