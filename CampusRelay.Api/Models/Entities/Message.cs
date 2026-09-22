using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

/// <summary>messages - one chat line tied to a transaction.</summary>
public class Message
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    [Required]
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
