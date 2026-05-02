using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Post34.Helpers;
using Post34.Repositories;
using Post34.Models;
using System.Text.Json;
using MongoDB.Bson;

namespace Post34.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiDefinitionsController : ControllerBase
{
    private readonly JwtSettings _jwt;
    private readonly IUserRepository _userRepo;
    private readonly IApiDefinitionRepository _apiRepo;

    public ApiDefinitionsController(IOptions<JwtSettings> jwtOptions, IUserRepository userRepo, IApiDefinitionRepository apiRepo)
    {
        _jwt = jwtOptions.Value;
        _userRepo = userRepo;
        _apiRepo = apiRepo;
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetApiDefinitionsByProject(string projectId)
    {
        // Simple token validation - just check if token is provided
        var token = GetSimpleToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Token required." });
        }

        try
        {
            var apiDefinitions = await _apiRepo.GetByParentProjectIdAsync(projectId);

            var responseData = apiDefinitions.Select(api => new
            {
                Id = api.Id,
                parent_project_id = api.parent_project_id,
                api_name = api.api_name,
                method = api.method,
                url = api.url,
                description = api.description,
                request_body = api.request_body?.ToJson()
            }).ToList();

            return Ok(new { status = StatusCodes.Status200OK, data = responseData });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Failed to fetch API definitions.", error = ex.Message });
        }
    }

    [HttpGet("services/{parentProjectId}")]
    public async Task<IActionResult> GetServicesByParentProjectId(string parentProjectId)
    {
        // Simple token validation - just check if token is provided
        var token = GetSimpleToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Token required." });
        }

        try
        {
            var services = await _apiRepo.GetByParentProjectIdAsync(parentProjectId);

            var responseData = services.Select(api => new
            {
                Id = api.Id,
                parent_project_id = api.parent_project_id,
                api_name = api.api_name,
                method = api.method,
                url = api.url,
                description = api.description,
                request_body = api.request_body?.ToJson()
            }).ToList();

            return Ok(new { status = StatusCodes.Status200OK, data = responseData });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Failed to fetch services by parent_project_id.", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiDefinition(string id)
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Missing JWT token." });
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

            var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Unable to determine user from token." });

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "User not found." });

            var apiDefinition = await _apiRepo.GetByIdAsync(id);

            if (apiDefinition == null)
                return NotFound(new { status = StatusCodes.Status404NotFound, message = "API definition not found." });

            var responseData = new
            {
                Id = apiDefinition.Id,
                parent_project_id = apiDefinition.parent_project_id,
                api_name = apiDefinition.api_name,
                method = apiDefinition.method,
                url = apiDefinition.url,
                description = apiDefinition.description,
                request_body = apiDefinition.request_body?.ToJson()
            };

            return Ok(new { status = StatusCodes.Status200OK, data = responseData });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Invalid or expired token.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiDefinition([FromBody] ApiDefinitionCreateRequest request)
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Missing JWT token." });
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

            var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Unable to determine user from token." });

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "User not found." });

            BsonDocument? parsedRequestBody = null;
            if (request.request_body != null)
            {
                try
                {
                    parsedRequestBody = BsonDocument.Parse(request.request_body);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Invalid request_body JSON.", error = ex.Message });
                }
            }

            var apiDefinition = new ApiDefinition
            {
                parent_project_id = request.parent_project_id,
                api_name = request.api_name,
                method = request.method,
                url = request.url,
                description = request.description,
                request_body = parsedRequestBody
            };

            await _apiRepo.CreateAsync(apiDefinition);

            var responseData = new
            {
                Id = apiDefinition.Id,
                parent_project_id = apiDefinition.parent_project_id,
                api_name = apiDefinition.api_name,
                method = apiDefinition.method,
                url = apiDefinition.url,
                description = apiDefinition.description,
                request_body = apiDefinition.request_body?.ToJson()
            };

            return CreatedAtAction(nameof(GetApiDefinition), new { id = apiDefinition.Id },
                new { status = StatusCodes.Status201Created, data = responseData });
        }
        catch (SecurityTokenMalformedException ex)
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Invalid JWT token format. Token must be a valid JWT with proper structure (header.payload.signature).", error = ex.Message });
        }
        catch (SecurityTokenExpiredException ex)
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "JWT token has expired.", error = ex.Message });
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Invalid JWT token signature.", error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Failed to create API definition.", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiDefinition(string id, [FromBody] ApiDefinitionUpdateRequest request)
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Missing JWT token." });
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

            var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Unable to determine user from token." });

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "User not found." });

            var existingApi = await _apiRepo.GetByIdAsync(id);
            if (existingApi == null)
                return NotFound(new { status = StatusCodes.Status404NotFound, message = "API definition not found." });

            BsonDocument? parsedRequestBody = existingApi.request_body;
            if (request.request_body != null)
            {
                try
                {
                    parsedRequestBody = BsonDocument.Parse(request.request_body);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Invalid request_body JSON.", error = ex.Message });
                }
            }

            var updatedApi = new ApiDefinition
            {
                Id = existingApi.Id,
                parent_project_id = request.parent_project_id ?? existingApi.parent_project_id,
                api_name = request.api_name ?? existingApi.api_name,
                method = request.method ?? existingApi.method,
                url = request.url ?? existingApi.url,
                description = request.description ?? existingApi.description,
                request_body = parsedRequestBody
            };

            await _apiRepo.UpdateAsync(id, updatedApi);

            var responseData = new
            {
                Id = updatedApi.Id,
                parent_project_id = updatedApi.parent_project_id,
                api_name = updatedApi.api_name,
                method = updatedApi.method,
                url = updatedApi.url,
                description = updatedApi.description,
                request_body = updatedApi.request_body?.ToJson()
            };

            return Ok(new { status = StatusCodes.Status200OK, data = responseData });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Failed to update API definition.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiDefinition(string id)
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Missing JWT token." });
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

            var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Unable to determine user from token." });

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "User not found." });

            var existingApi = await _apiRepo.GetByIdAsync(id);
            if (existingApi == null)
                return NotFound(new { status = StatusCodes.Status404NotFound, message = "API definition not found." });

            await _apiRepo.DeleteAsync(id);

            return Ok(new { status = StatusCodes.Status200OK, message = "API definition deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Failed to delete API definition.", error = ex.Message });
        }
    }

    private string? GetAuthToken()
    {
        // Try j_token header first
        if (Request.Headers.TryGetValue("j_token", out var tokenVals) && !string.IsNullOrWhiteSpace(tokenVals.First()))
        {
            var rawToken = tokenVals.First();
            var token = NormalizeBearerToken(rawToken);
            Console.WriteLine($"DEBUG: Found token in j_token header: {token?.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
            return token;
        }

        // Try Authorization header
        if (Request.Headers.TryGetValue("Authorization", out var authVals))
        {
            var authHeader = authVals.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                var token = NormalizeBearerToken(authHeader);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine($"DEBUG: Found token in Authorization header: {token?.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
                    return token;
                }
            }
        }

        // Try token query parameter
        if (Request.Query.TryGetValue("token", out var queryToken) && !string.IsNullOrWhiteSpace(queryToken.First()))
        {
            var rawToken = queryToken.First();
            var token = NormalizeBearerToken(rawToken);
            Console.WriteLine($"DEBUG: Found token in query parameter: {token?.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
            return token;
        }

        Console.WriteLine("DEBUG: No token found in any location");
        return null;
    }

    private string? GetSimpleToken()
    {
        // Try j_token header first
        if (Request.Headers.TryGetValue("j_token", out var tokenVals) && !string.IsNullOrWhiteSpace(tokenVals.First()))
        {
            return NormalizeBearerToken(tokenVals.First());
        }

        // Try Authorization header
        if (Request.Headers.TryGetValue("Authorization", out var authVals))
        {
            var authHeader = authVals.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                return NormalizeBearerToken(authHeader);
            }
        }

        // Try token query parameter
        if (Request.Query.TryGetValue("token", out var queryToken) && !string.IsNullOrWhiteSpace(queryToken.First()))
        {
            return NormalizeBearerToken(queryToken.First());
        }

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
}

public class ApiDefinitionCreateRequest
{
    public string parent_project_id { get; set; } = string.Empty;
    public string api_name { get; set; } = string.Empty;
    public string method { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string? request_body { get; set; }
}

public class ApiDefinitionUpdateRequest
{
    public string? parent_project_id { get; set; }
    public string? api_name { get; set; }
    public string? method { get; set; }
    public string? url { get; set; }
    public string? description { get; set; }
    public string? request_body { get; set; }
}