using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreeService.Contracts.Auth;
using TreeService.Domain.Abstractions;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt: {Username}", request.Username);
            
            var response = await _authService.LoginAsync(request);
            
            _logger.LogInformation("Login successful: {Username}", 
                request.Username);
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Username}, Reason: {Message}", 
                request.Username, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error: {Username}", request.Username);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Registration: {Username}", request.Username);
            
            var userId = await _authService.RegisterAsync(request);
            
            _logger.LogInformation("User registered: {Username}, ID: {Id}", 
                request.Username, userId);
            
            return Ok(userId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration failed: {Username}, Reason: {Message}", 
                request.Username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error: {Username}", request.Username);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPost("register-admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<User>> RegisterAdmin(LoginRequest request)
    {
        var adminName = User.Identity?.Name ?? "unknown";
        
        try
        {
            _logger.LogInformation("Admin registration by {Admin}: {Username}", 
                adminName, request.Username);
            
            var userId = await _authService.RegisterAsync(request, "Administrator");
            
            _logger.LogInformation("Admin created by {Admin}: {Username}", 
                adminName, request.Username);
            
            return Ok(userId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Admin creation failed by {Admin}: {Username}, Reason: {Message}", 
                adminName, request.Username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin creation error by {Admin}: {Username}", 
                adminName, request.Username);
            return StatusCode(500, "Internal server error");
        }
    }
}