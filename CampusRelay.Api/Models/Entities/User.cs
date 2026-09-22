using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

/// <summary>
/// users - mirrors Part 1's "User (users)" schema. Created on first successful
/// SSO login (REQ-AUTH-3): student id / email / full name come from the SSO token's claims.
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Unique subject claim from the university SSO token.</summary>
    [Required, MaxLength(256)]
    public string SsoSub { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public int RatingSum { get; set; }
    public int RatingCount { get; set; }

    /// <summary>REQ-ECO-2: cumulative CO2 (grams) saved across completed transactions.</summary>
    public int EcoScore { get; set; }

    public double WalletBalance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
