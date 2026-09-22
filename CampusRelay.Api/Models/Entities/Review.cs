using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

/// <summary>reviews - one rating per transaction (Part 1: transaction_id is unique).</summary>
public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    [Required]
    public Guid ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    [Required]
    public Guid RevieweeId { get; set; }
    public User? Reviewee { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
