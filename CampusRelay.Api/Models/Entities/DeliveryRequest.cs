using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

/// <summary>Small/Medium/Large, exactly as the Delivery Request Creation Wizard's
/// radio group sends it (REQ-DEL-1's Endpoint 1 payload: "weightCategory").</summary>
public enum WeightCategory { Small, Medium, Large }

/// <summary>Part 1's schema section lists ACTIVE/MATCHED/FULFILLED/CANCELLED; the
/// endpoint example additionally shows "PendingCourier" for a brand-new request.
/// This prototype keeps a single, consistent status set (both here and on the
/// Android client) and treats a fresh request as Active-awaiting-a-courier.</summary>
public enum DeliveryStatus { Active, Matched, Fulfilled, Cancelled }

/// <summary>
/// delivery_requests - REQ-DEL-1. Combines Part 1's literal Endpoint 1 JSON contract
/// (building names, weightCategory, rewardAmount) with its schema section's geospatial
/// fields (pickup/dropoff lat/lng), since the two describe the same table slightly
/// differently. The lat/lng fields are nullable until REQ-DEL-3's building -&gt; coordinate
/// lookup and campus map integration exist.
/// </summary>
public class DeliveryRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    [Required, MaxLength(500)]
    public string ItemDescription { get; set; } = string.Empty;

    /// <summary>JSON-encoded list of photo URLs. Part 1 calls this "JSONB"; stored as a
    /// JSON-in-NVARCHAR column here since the target DB is Azure SQL (SQL Server), which
    /// has no native JSONB type - see JsonStringListConverter.</summary>
    public List<string> ItemPhotoUrls { get; set; } = new();

    public WeightCategory WeightCategory { get; set; }

    /// <summary>Derived from WeightCategory (mirrors the Android client's
    /// WeightCategory.approxKg) so this still lines up with Part 1's weight_kg column.</summary>
    public double WeightKg { get; set; }

    [Required, MaxLength(200)]
    public string PickupBuilding { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string DropoffBuilding { get; set; } = string.Empty;

    public double? PickupLat { get; set; }
    public double? PickupLng { get; set; }
    public double? DropoffLat { get; set; }
    public double? DropoffLng { get; set; }

    public DateTime? WindowStart { get; set; }
    public DateTime? WindowEnd { get; set; }

    public double RewardAmount { get; set; }
    public bool IsEcoFriendlyRoute { get; set; } = true;

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Active;

    [MaxLength(100)]
    public string QrVerificationCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
