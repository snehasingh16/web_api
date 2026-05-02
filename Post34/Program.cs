using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MongoDB.Bson;
using Post34.Data;
using Post34.Helpers;
using Post34.Repositories;
using Post34.Services;
using Post34.DTOs;
using Post34.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// configure DbContext (InMemory for demo; replace with SqlServer/Postgres in prod)
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("Post34"));

// bind JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

// bind mongo settings from configuration (use appsettings.json)
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
var mongoSettings = builder.Configuration.GetSection("Mongo").Get<MongoSettings>() ?? new MongoSettings();


// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

// DI
builder.Services.AddSingleton(mongoSettings);

// Register Mongo-backed user repository
builder.Services.AddScoped<IUserRepository, MongoUserRepository>();

// Register Mongo-backed project repository
builder.Services.AddScoped<IProjectRepository, MongoProjectRepository>();

// Register Mongo-backed API definition repository
builder.Services.AddScoped<IApiDefinitionRepository, MongoApiDefinitionRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Log which user repository implementation was selected
app.Logger.LogInformation("Startup: using MongoUserRepository (MongoDB)");

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// seed data (development convenience) from configuration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var mongoConfig = scope.ServiceProvider.GetRequiredService<MongoSettings>();

    // ensure a default user exists (keeps previous behaviour)
    if (!db.Users.Any())
    {
        var user = new Post34.Models.User
        {
            Id = "1",
            username = "test",
            passwordHash = AuthService.HashPassword("P@ssw0rd"),
            role = "Admin"
        };
        db.Users.Add(user);
        db.SaveChanges();
    }

    // seed project permissions
    if (!db.ProjectPermissions.Any())
    {
        db.ProjectPermissions.Add(new ProjectPermission { UserId = "1", ProjectId = 1777651609, CanAccess = true });
        db.ProjectPermissions.Add(new ProjectPermission { UserId = "1", ProjectId = 1777651902, CanAccess = true });
        db.ProjectPermissions.Add(new ProjectPermission { UserId = "2", ProjectId = 1777651990, CanAccess = true });
        db.SaveChanges();
    }

    // seed users in MongoDB
    var client = new MongoClient(mongoConfig.ConnectionString);
    var mongoDb = client.GetDatabase(mongoConfig.Database);
    mongoDb.DropCollection("Users");
    var userCollection = mongoDb.GetCollection<BsonDocument>("Users");
    var userDoc = new BsonDocument
    {
        { "_id", "1" },
        { "username", "test" },
        { "passwordHash", AuthService.HashPassword("P@ssw0rd") },
        { "role", "Admin" }
    };
    userCollection.InsertOne(userDoc);
}

app.Run();


