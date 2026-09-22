using CampusRelay.Api.Data;
using CampusRelay.Api.Models.Dtos;
using CampusRelay.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRelay.Api.Controllers;

/// <summary>
/// REQ-DEL-1 / REQ-OFF-2: the two endpoints Part 1 documents byte-for-byte under
/// "REST Api and Backend Architecture". This is what the Android app's
/// DeliveryRepositoryImpl / ApiService call.
/// </summary>
[ApiController]
[Route("api/v1/deliveries")]
[Authorize] // REQ-API-1: every call needs Authorization: Bearer <JWT_TOKEN>
public class DeliveriesController : ControllerBase
{
    private readonly CampusRelayDbContext _db;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(CampusRelayDbContext db, ILogger<DeliveriesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>POST /api/v1/deliveries</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateDeliveryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDeliveryRequest([FromBody] CreateDeliveryRequestDto dto)
    {
        if (!Guid.TryParse(dto.SenderId, out var senderId) ||
            !await _db.Users.AnyAsync(u => u.Id == senderId))
        {
            return BadRequest(new { message = "senderId must be an existing user id" });
        }

        if (!Enum.TryParse<WeightCategory>(dto.WeightCategory, ignoreCase: true, out var weightCategory))
        {
            return BadRequest(new { message = "weightCategory must be Small, Medium or Large" });
        }

        if (string.IsNullOrWhiteSpace(dto.PickupBuilding) || string.IsNullOrWhiteSpace(dto.DropoffBuilding))
        {
            return BadRequest(new { message = "pickupBuilding and dropoffBuilding are required" });
        }

        var entity = new DeliveryRequest
        {
            RequesterId = senderId,
            PickupBuilding = dto.PickupBuilding,
            DropoffBuilding = dto.DropoffBuilding,
            ItemDescription = dto.ItemDescription,
            WeightCategory = weightCategory,
            WeightKg = ApproxKgFor(weightCategory),
            RewardAmount = dto.RewardAmount,
            IsEcoFriendlyRoute = dto.IsEcoFriendlyRoute,
            Status = DeliveryStatus.Active,
            QrVerificationCode = $"HASH_{Random.Shared.Next(10000, 99999)}_TEMP"
        };

        _db.DeliveryRequests.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Delivery request {Id} created by {SenderId}", entity.Id, senderId);

        var response = new CreateDeliveryResponseDto(
            DeliveryId: entity.Id.ToString(),
            Status: entity.Status.ToString(),
            CreatedAt: entity.CreatedAt,
            QrVerificationCode: entity.QrVerificationCode
        );
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    /// <summary>GET /api/v1/deliveries/{id} - not in Part 1's spec, added so
    /// CreatedAtAction above has somewhere valid to point at, and so a future
    /// "delivery details" screen has something to call.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _db.DeliveryRequests.FindAsync(id);
        return entity is null ? NotFound() : Ok(entity);
    }

    /// <summary>POST /api/v1/deliveries/sync-offline</summary>
    [HttpPost("sync-offline")]
    [ProducesResponseType(typeof(SyncOfflineResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncOfflineTransactions([FromBody] SyncOfflineRequestDto dto)
    {
        // REQ-OFF-2: each queued handoff references a delivery + the QR hash scanned at
        // handoff time. Accepted if the delivery exists and the hash matches, then the
        // delivery is marked Fulfilled. A full implementation would also write a
        // Transaction/Review row per handoff - left as a follow-up once REQ-CAR/REQ-DEL
        // matching produces real Transaction rows to attach them to.
        var failed = new List<string>();
        var syncedCount = 0;

        foreach (var tx in dto.OfflineTransactions)
        {
            if (!Guid.TryParse(tx.DeliveryId, out var deliveryId))
            {
                failed.Add(tx.LocalTransactionId);
                continue;
            }

            var delivery = await _db.DeliveryRequests.FindAsync(deliveryId);
            if (delivery is null || delivery.QrVerificationCode != tx.ScannedQrHash)
            {
                failed.Add(tx.LocalTransactionId);
                continue;
            }

            delivery.Status = DeliveryStatus.Fulfilled;
            syncedCount++;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Offline sync: {Synced} succeeded, {Failed} failed", syncedCount, failed.Count);

        return Ok(new SyncOfflineResponseDto(syncedCount, failed));
    }

    
    [AllowAnonymous]
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed()
    {
        var items = await _db.DeliveryRequests.Select(d => new {
            deliveryId = d.Id.ToString(),
            itemDescription = d.ItemDescription,
            pickupBuilding = d.PickupBuilding,
            dropoffBuilding = d.DropoffBuilding,
            rewardAmount = d.RewardAmount,
            distanceKm = 1.0,
            status = d.Status.ToString()
        }).ToListAsync();

        return Ok(items);
    }

private static double ApproxKgFor(WeightCategory category) => category switch
    {
        WeightCategory.Small => 0.5,
        WeightCategory.Medium => 3.0,
        WeightCategory.Large => 8.0,
        _ => 0
    };
}
