using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Post34.Data;
using Post34.Helpers;
using Post34.Repositories;
using Post34.Services;
using Post34.DTOs;

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

// bind seed settings
builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection("SeedData"));
var seed = builder.Configuration.GetSection("SeedData").Get<SeedSettings>() ?? new SeedSettings();

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

// Register Mongo-backed user repository (remove EF InMemory repository)
builder.Services.AddScoped<IUserRepository, MongoUserRepository>();

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

    // ensure a default user exists (keeps previous behaviour)
    if (!db.Users.Any())
    {
            var user = new Post34.Models.User
            {
                Id = Guid.NewGuid().ToString(),
                username = "test",
                passwordHash = AuthService.HashPassword("P@ssw0rd"),
                role = "Admin"
            };
        db.Users.Add(user);
        db.SaveChanges();
    }

    // seed projects from configuration (if any)
    if (seed.Projects != null && seed.Projects.Any())
    {
        foreach (var sp in seed.Projects)
        {
            if (!db.Projects.Any(p => p.project_name == sp.Name && p.project_id == sp.project_id))
            {
                db.Projects.Add(new Post34.Models.Project { project_name = sp.Name, project_id = sp.project_id, used_services_list = sp.used_services_list });
            }
        }
        db.SaveChanges();
    }

    // seed permissions from configuration
    if (seed.Permissions != null && seed.Permissions.Any())
    {
                foreach (var perm in seed.Permissions)
                {
                    var user = db.Users.FirstOrDefault(u => u.username == perm.Username);
                    var project = db.Projects.FirstOrDefault(p => p.project_name == perm.ProjectName);
                    if (user != null && project != null)
                    {
                        if (!db.ProjectPermissions.Any(pp => pp.UserId == user.Id && pp.ProjectId == project.project_id))
                        {
                            db.ProjectPermissions.Add(new Post34.Models.ProjectPermission { UserId = user.Id, ProjectId = project.project_id, CanAccess = perm.CanAccess });
                        }
                    }
                }
        db.SaveChanges();
    }
}

app.Run();


