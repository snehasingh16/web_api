using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Post34.Data;
using Post34.Helpers;
using Post34.Repositories;
using Post34.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// configure DbContext (InMemory for demo; replace with SqlServer/Postgres in prod)
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("Post34Db"));

// bind JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// keep a simple demo endpoint
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

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
            Username = "test",
            PasswordHash = AuthService.HashPassword("P@ssw0rd"),
            Role = "Admin"
        };
        db.Users.Add(user);
        db.SaveChanges();
    }

    // seed projects from configuration (if any)
    if (seed.Projects != null && seed.Projects.Any())
    {
        foreach (var sp in seed.Projects)
        {
            if (!db.Projects.Any(p => p.Name == sp.Name))
            {
                db.Projects.Add(new Post34.Models.Project { Name = sp.Name });
            }
        }
        db.SaveChanges();
    }

    // seed permissions from configuration
    if (seed.Permissions != null && seed.Permissions.Any())
    {
        foreach (var perm in seed.Permissions)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == perm.Username);
            var project = db.Projects.FirstOrDefault(p => p.Name == perm.ProjectName);
            if (user != null && project != null)
            {
                if (!db.ProjectPermissions.Any(pp => pp.UserId == user.Id && pp.ProjectId == project.Id))
                {
                    db.ProjectPermissions.Add(new Post34.Models.ProjectPermission { UserId = user.Id, ProjectId = project.Id, CanAccess = perm.CanAccess });
                }
            }
        }
        db.SaveChanges();
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

