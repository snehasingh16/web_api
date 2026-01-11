using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Post34.DTOs;
using Post34.Models;
using Post34.Repositories;
using Post34.Services;

namespace Post34.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepo;

    public AuthController(IAuthService authService, IUserRepository userRepo)
    {
        _authService = authService;
        _userRepo = userRepo;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Username and password are required." });
        }

        var auth = await _authService.LoginAsync(req);
        if (auth == null)
        {
            return Unauthorized(new { status = StatusCodes.Status401Unauthorized, message = "Invalid username or password." });
        }

        return Ok(new { status = StatusCodes.Status200OK, message = "Login successful.", j_token = auth.Token, expiresAt = auth.ExpiresAt });
    }

    // helper endpoint to create a test user (for demo/dev only)
    [HttpPost("register_user")]
    public async Task<IActionResult> RegisterTest(LoginRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            return BadRequest(new { status = StatusCodes.Status400BadRequest, message = "Username and password are required." });
        }

        var existing = await _userRepo.GetByUsernameAsync(req.Username);
        if (existing != null)
        {
            return Conflict(new { status = StatusCodes.Status409Conflict, message = "User already exists." });
        }

        var user = new User
        {
            username = req.Username,
            passwordHash = AuthService.HashPassword(req.Password),
            role = "user"
        };

        await _userRepo.AddAsync(user);
        return StatusCode(StatusCodes.Status201Created, new { status = StatusCodes.Status201Created, message = "User created successfully." });
    }
}
