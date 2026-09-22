using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CampusRelay.Api.Data;

/// <summary>
/// Stores a List&lt;string&gt; as a single JSON-encoded NVARCHAR column - the SQL Server /
/// SQLite equivalent of the JSONB columns Part 1's schema calls for (item_photo_urls,
/// photo_urls). Used by DeliveryRequest and MarketplaceListing in
/// CampusRelayDbContext.OnModelCreating.
/// </summary>
public static class JsonStringListConverter
{
    public static readonly ValueConverter<List<string>, string> Converter = new(
        list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>()
    );

    public static readonly ValueComparer<List<string>> Comparer = new(
        (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
        list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        list => list.ToList()
    );
}
