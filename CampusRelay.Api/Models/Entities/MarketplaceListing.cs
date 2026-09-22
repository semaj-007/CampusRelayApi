using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

public enum ListingCategory { Books, Furniture, Electronics, Clothing, Other }
public enum ListingCondition { New, LikeNew, Good, Fair }
public enum ListingStatus { Active, Pending, Sold, Expired, Hold }

/// <summary>marketplace_listings - mirrors Part 1's "Marketplace Listing" schema.</summary>
public class MarketplaceListing
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public double Price { get; set; }
    public ListingCategory Category { get; set; }
    public ListingCondition Condition { get; set; }

    /// <summary>JSON-encoded list - see JsonStringListConverter.</summary>
    public List<string> PhotoUrls { get; set; } = new();

    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
