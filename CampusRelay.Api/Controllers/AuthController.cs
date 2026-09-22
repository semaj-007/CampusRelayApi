using CampusRelay.Api.Data;
using CampusRelay.Api.Models.Dtos;
using CampusRelay.Api.Models.Entities;
using CampusRelay.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRelay.Api.Controllers;

/// <summary>
/// PROTOTYPE-ONLY. REQ-AUTH-1 calls for real OIDC against the university's SSO
/// (Microsoft Azure AD / Google) - that needs a real App Registration and isn't
/// something that can be stood up without those credentials. This controller exists
/// purely so the Android app and Swagger UI have a way to get a bearer token and a
/// User row while that integration is being built. Delete it once REQ-AUTH-1 is real.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly CampusRelayDbContext _db;
    private readonly IJwtTokenService _tokenService;

    public AuthController(CampusRelayDbContext db, IJwtTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }
       
    [HttpPost("sso")]
        public async Task<ActionResult<AuthResponseDto>> Sso([FromBody] SsoLoginRequestDto dto)
        {
            var email = $"user_{Guid.NewGuid():N}@campusrelay.local";

            var user = new User
            {
                SsoSub = dto.IdToken ?? Guid.NewGuid().ToString(),
                Email = email,
                FullName = "Campus User"
            };

            var existing = await _db.Users
                .FirstOrDefaultAsync(u => u.SsoSub == user.SsoSub);

            if (existing == null)
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            else
            {
                user = existing;
            }

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto(
                token,
                user.Id.ToString(),
                user.FullName,
                user.Email));
        }

    

    /// <summary>POST /api/v1/auth/dev-login - upserts a User row (mimicking REQ-AUTH-3's
    /// first-login profile creation) and returns a bearer token for it.</summary>
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
