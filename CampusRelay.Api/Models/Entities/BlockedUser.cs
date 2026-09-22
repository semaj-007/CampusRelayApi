namespace CampusRelay.Api.Models.Entities;

/// <summary>blocked_users - composite primary key (blocker_id, blocked_id),
/// configured in CampusRelayDbContext.OnModelCreating since EF Core can't infer
/// a composite key from data annotations alone.</summary>
public class BlockedUser
{
    public Guid BlockerId { get; set; }
    public User? Blocker { get; set; }

    public Guid BlockedId { get; set; }
    public User? Blocked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
