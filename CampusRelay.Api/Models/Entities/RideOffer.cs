using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

public enum VehicleType { Car, Suv, Van }
public enum RideOfferStatus { Active, Full, Cancelled }

/// <summary>ride_offers - REQ-CAR-1: a driver's "Ride Offer".</summary>
public class RideOffer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DriverId { get; set; }
    public User? Driver { get; set; }

    [Required, MaxLength(120)]
    public string OriginCity { get; set; } = string.Empty;
    [Required, MaxLength(120)]
    public string DestinationCity { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
    public VehicleType VehicleType { get; set; }

    /// <summary>Cost-sharing price, not a for-profit fare - matches Part 1's wording.</summary>
    public double PricePerSeat { get; set; }

    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }

    public RideOfferStatus Status { get; set; } = RideOfferStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
