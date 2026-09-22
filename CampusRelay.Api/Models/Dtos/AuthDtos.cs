namespace CampusRelay.Api.Models.Dtos;

/// <summary>PROTOTYPE-ONLY - see AuthController's class doc comment. Not part of
/// Part 1's documented REST surface; exists purely to unblock local testing while
/// REQ-AUTH-1's real OIDC flow isn't wired up yet.</summary>
public record DevLoginRequestDto(string Email, string FullName);

public record DevLoginResponseDto(string AccessToken, string UserId, string FullName);


public record SsoLoginRequestDto(string Provider, string IdToken);

public record AuthResponseDto(
    string AccessToken,
    string UserId,
    string FullName,
    string Email
);
