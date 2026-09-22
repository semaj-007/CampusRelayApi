namespace CampusRelay.Api.Models.Dtos;

/// <summary>
/// Request body for POST /api/v1/deliveries - field names copied verbatim from Part 1's
/// "Endpoint 1: Create Delivery Request" spec. Program.cs configures camelCase JSON
/// naming, so these PascalCase properties serialize/deserialize as senderId,
/// pickupBuilding, etc. exactly as documented - no [JsonPropertyName] needed.
/// </summary>
public record CreateDeliveryRequestDto(
    string SenderId,
    string PickupBuilding,
    string DropoffBuilding,
    string ItemDescription,
    string WeightCategory,
    double RewardAmount,
    bool IsEcoFriendlyRoute
);

/// <summary>Response body (201 Created) for the same endpoint.</summary>
public record CreateDeliveryResponseDto(
    string DeliveryId,
    string Status,
    DateTime CreatedAt,
    string QrVerificationCode
);
