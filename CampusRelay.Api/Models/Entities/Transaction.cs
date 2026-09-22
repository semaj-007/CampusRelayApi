using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

public enum TransactionType { Delivery, Marketplace, Carpool }
public enum TransactionStatus { Pending, Accepted, InProgress, Completed, Cancelled, Disputed }
public enum EscrowStatus { NotFunded, Funded, Released, Refunded }

/// <summary>
/// transactions - the shared record for a completed deal, whichever of the three
/// service types produced it (Part 1: "polymorphic foreign key" via ServiceRefId).
/// </summary>
public class Transaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [Required]
    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    [Required]
    public Guid ProviderId { get; set; }
    public User? Provider { get; set; }

    /// <summary>Points at a DeliveryRequest, MarketplaceListing, or RideOffer id
    /// depending on Type. Deliberately not a real DB foreign key (it targets a
    /// different table per row) - resolve it in application code by Type.</summary>
    public Guid ServiceRefId { get; set; }

    public double AgreedPrice { get; set; }
    public EscrowStatus EscrowStatus { get; set; } = EscrowStatus.NotFunded;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
