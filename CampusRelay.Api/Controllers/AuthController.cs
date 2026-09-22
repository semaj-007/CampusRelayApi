using CampusRelay.Api.Data;
using CampusRelay.Api.Models.Dtos;
using CampusRelay.Api.Models.Entities;
using CampusRelay.Api.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRelay.Api.Controllers;

/// <summary>
/// Authentication controller that handles Firebase-based SSO login.
/// REQ-AUTH-1: Real SSO via Firebase Authentication.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly CampusRelayDbContext _db;
    private readonly IJwtTokenService _tokenService;
    private readonly IFirebaseTokenValidator _firebaseTokenValidator;

    public AuthController(
        CampusRelayDbContext db,
        IJwtTokenService tokenService,
        IFirebaseTokenValidator firebaseTokenValidator)
    {
        _db = db;
        _tokenService = tokenService;
        _firebaseTokenValidator = firebaseTokenValidator;
    }

    /// <summary>
    /// POST /api/v1/auth/sso
    /// Validates Firebase ID token and returns a JWT for API access.
    /// </summary>
    [HttpPost("sso")]
    public async Task<ActionResult<AuthResponseDto>> Sso([FromBody] SsoLoginRequestDto dto)
    {
        try
        {
            // Validate Firebase ID token
            var firebaseToken = await _firebaseTokenValidator.ValidateTokenAsync(dto.IdToken);

            // Create or find user based on Firebase UID
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.SsoSub == firebaseToken.Uid);

            if (user == null)
            {
                user = new User
                {
                    SsoSub = firebaseToken.Uid,
                    Email = firebaseToken.Claims?.GetValueOrDefault("email")?.ToString() ?? $"{firebaseToken.Uid}@firebase.local",
                    FullName = firebaseToken.Claims?.GetValueOrDefault("name")?.ToString() ?? "Firebase User"
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            // Generate JWT token for API access
            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto(
                token,
                user.Id.ToString(),
                user.FullName,
                user.Email));
        }
        catch (FirebaseAuthException ex)
        {
            return BadRequest(new { message = "Invalid Firebase token", error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Authentication failed", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/v1/auth/dev-login - For development/testing only.
    /// Creates or finds a user and returns a JWT token.
    /// </summary>
    [HttpPost("dev-login")]
    public async Task<ActionResult<DevLoginResponseDto>> DevLogin([FromBody] DevLoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "email and fullName are required" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            user = new User
            {
                SsoSub = $"dev|{dto.Email}",
                Email = dto.Email,
                FullName = dto.FullName
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(new DevLoginResponseDto(token, user.Id.ToString(), user.FullName));
    }
}
