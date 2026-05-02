using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Post34.DTOs;
using Post34.Helpers;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Post34.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly JwtSettings _jwt;
    private readonly Post34.Data.AppDbContext _db;
    private readonly Post34.Repositories.IUserRepository _userRepo;
    private readonly Post34.Repositories.IProjectRepository _projectRepo;

    public ProjectsController(Post34.Data.AppDbContext db, IOptions<JwtSettings> jwtOptions, Post34.Repositories.IUserRepository userRepo, Post34.Repositories.IProjectRepository projectRepo)
    {
        _db = db;
        _jwt = jwtOptions.Value;
        _userRepo = userRepo;
        _projectRepo = projectRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Missing JWT token. Use j_token header or Authorization: Bearer <token>." });
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Key);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwt.Issuer,
                ValidAudience = _jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            // extract username from token claims
            var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Unable to determine user from token." });

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "User not found." });

            // Fetch the single document from MongoDB
            var mongoSettings = HttpContext.RequestServices.GetRequiredService<Post34.Helpers.MongoSettings>();
            var client = new MongoClient(mongoSettings.ConnectionString);
            var db = client.GetDatabase(mongoSettings.Database);
            var collection = db.GetCollection<BsonDocument>("Projects");
            var doc = await collection.Find(_ => true).FirstOrDefaultAsync();

            if (doc != null)
            {
                List<object> projects = new List<object>();
                if (doc.Contains("projects"))
                {
                    projects = doc["projects"].AsBsonArray.Select(p =>
                    {
                        var projectDoc = p.AsBsonDocument;
                        return (object)new
                        {
                            project_id = projectDoc.GetValue("project_id", BsonNull.Value).AsInt32,
                            project_name = projectDoc.GetValue("project_name", BsonNull.Value).AsString,
                            proj_description = projectDoc.GetValue("proj_description", BsonNull.Value).AsString,
                            proj_permission = projectDoc.GetValue("proj_permission", BsonNull.Value).ToString()
                        };
                    }).ToList();
                }

                List<object> user_permissions = new List<object>();
                if (doc.Contains("user_permissions"))
                {
                    user_permissions = doc["user_permissions"].AsBsonArray
                        .Select(up => up.AsBsonDocument)
                        .GroupBy(up => up.GetValue("user_id", BsonNull.Value).ToString())
                        .Select(g => (object)new
                        {
                            user_id = g.Key,
                            projects = g
                                .Where(up => up.Contains("project_id"))
                                .Select(up => up.GetValue("project_id", BsonNull.Value).AsInt32)
                                .ToList()
                        })
                        .ToList();
                }

                return Ok(new { projects = projects, user_permissions = user_permissions });
            }
            else
            {
                return Ok(new { projects = new List<object>(), user_permissions = new List<object>() });
            }
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Invalid or expired token.", error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Failed to read project data.", error = ex.Message });
        }
    }

    private string? GetAuthToken()
    {
        if (Request.Headers.TryGetValue("j_token", out var tokenVals) && !string.IsNullOrWhiteSpace(tokenVals.First()))
            return NormalizeBearerToken(tokenVals.First());

        if (Request.Headers.TryGetValue("Authorization", out var authVals))
        {
            var authHeader = authVals.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader))
                return NormalizeBearerToken(authHeader);
        }

        if (Request.Query.TryGetValue("token", out var queryToken) && !string.IsNullOrWhiteSpace(queryToken.First()))
            return NormalizeBearerToken(queryToken.First());

        return null;
    }

    private static string? NormalizeBearerToken(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;

        const string bearerPrefix = "Bearer ";
        if (rawToken.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return rawToken.Substring(bearerPrefix.Length).Trim();

        return rawToken.Trim();
    }

    private int ConvertObjectIdToInt(string? objectId)
    {
        if (string.IsNullOrEmpty(objectId) || objectId.Length < 8)
            return 0;
        
        // Take first 8 hex characters and convert to int
        var hex = objectId.Substring(0, 8);
        try
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }
        catch
        {
            return 0;
        }
    }
}
