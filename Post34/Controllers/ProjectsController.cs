using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Post34.DTOs;
using Post34.Helpers;

namespace Post34.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly JwtSettings _jwt;
    private readonly Post34.Data.AppDbContext _db;
    private readonly Post34.Repositories.IUserRepository _userRepo;

    public ProjectsController(Post34.Data.AppDbContext db, IOptions<JwtSettings> jwtOptions, Post34.Repositories.IUserRepository userRepo)
    {
        _db = db;
        _jwt = jwtOptions.Value;
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        // expect token in custom header `j_token`
        if (!Request.Headers.TryGetValue("j_token", out var tokenVals) || string.IsNullOrWhiteSpace(tokenVals.First()))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Missing j_token header." });
        }

        var token = tokenVals.First();

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

            // load permissions for user
            var perms = _db.ProjectPermissions
                .Where(pp => pp.UserId == user.Id)
                .ToDictionary(pp => pp.ProjectId, pp => pp.CanAccess);

            // fetch projects from DB and map to DTO with per-user permission
            var projList = _db.Projects.ToList();
            var projects = projList
                .Select(p => new ProjectDto
                {
                    ProjectId = p.project_id,
                    ProjectName = p.project_name,
                    used_services_list = p.used_services_list,
                    Permission = perms.TryGetValue(p.project_id, out var can) ? can : false,
                    
                })
                .ToList();

            return Ok(new { status = StatusCodes.Status200OK, data = projects });
        }
        catch
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Invalid or expired token." });
        }
    }
}
