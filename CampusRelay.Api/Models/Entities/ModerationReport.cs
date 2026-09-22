using System.ComponentModel.DataAnnotations;

namespace CampusRelay.Api.Models.Entities;

public enum ReportedItemType { User, Listing, Transaction }
public enum ReportReason { Spam, Fraud, Harassment, InappropriateContent, Other }
public enum ReportStatus { Open, UnderReview, Resolved, Dismissed }

/// <summary>moderation_reports - a user-filed report against a user, listing, or transaction.</summary>
public class ModerationReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ReporterId { get; set; }
    public User? Reporter { get; set; }

    public ReportedItemType ReportedItemType { get; set; }

    /// <summary>Id of the reported user/listing/transaction row - which table depends
    /// on ReportedItemType, same polymorphic pattern as Transaction.ServiceRefId.</summary>
    public Guid ReportedItemId { get; set; }

    public ReportReason ReasonCode { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
