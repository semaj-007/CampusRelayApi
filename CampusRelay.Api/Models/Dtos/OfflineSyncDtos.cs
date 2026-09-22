namespace CampusRelay.Api.Models.Dtos;

/// <summary>One queued handoff, as the Android client's OfflineTransactionEntity sends it
/// (REQ-OFF-2). Field names match Part 1's "Endpoint 2" spec exactly under camelCase JSON.</summary>
public record OfflineTransactionDto(
    string LocalTransactionId,
    string DeliveryId,
    string ScannedQrHash,
    DateTime CompletedAt,
    string CourierId
);

/// <summary>Request body for POST /api/v1/deliveries/sync-offline.</summary>
public record SyncOfflineRequestDto(List<OfflineTransactionDto> OfflineTransactions);

/// <summary>Response body (200 OK) for the same endpoint.</summary>
public record SyncOfflineResponseDto(int SyncedCount, List<string> FailedTransactions);
