
using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.Reservation;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Caching;

namespace Parky.Infrastructure.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IParkingRepository _parkingRepository;
    private readonly IMapper _mapper;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ICacheService _cacheService;
    public ReservationService(
    IReservationRepository reservationRepository,
    IParkingRepository parkingRepository,
    IMapper mapper,
    INotificationDispatcher notificationDispatcher,
    ICacheService cacheService)
    {
        _reservationRepository = reservationRepository;
        _parkingRepository = parkingRepository;
        _mapper = mapper;
        _notificationDispatcher = notificationDispatcher;
        _cacheService = cacheService;
    }

    public async Task<ReservationResponse> CreateAsync(
    CreateReservationRequest request,
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user is required.");
        }

        if (request.ParkingId == Guid.Empty ||
            request.VehicleId == Guid.Empty)
        {
            throw new ArgumentException(
                "ParkingId and VehicleId are required.");
        }

        var now = DateTimeOffset.UtcNow;

        if (request.StartTime <= now)
        {
            throw new ArgumentException(
                "Reservation start time must be in the future.");
        }

        if (request.EndTime <= request.StartTime)
        {
            throw new ArgumentException(
                "Reservation end time must be after its start time.");
        }

        var parking = await _parkingRepository.GetByIdAsync(
            request.ParkingId,
            cancellationToken);

        if (parking is null)
        {
            throw new KeyNotFoundException(
                "Parking was not found.");
        }

        var hasOverlap =
            await _reservationRepository.HasOverlappingReservationAsync(
                request.ParkingId,
                request.StartTime,
                request.EndTime,
                cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "The parking has an overlapping reservation.");
        }

        var reservationNumber = await GenerateReservationNumberAsync(
            cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ParkingId = request.ParkingId,
            VehicleId = request.VehicleId,
            ReservationNumber = reservationNumber,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalAmount = 0,
            DiscountAmount = 0,
            FinalAmount = 0,
            Status = "Pending",
            CreatedAt = now
        };

        await _reservationRepository.AddAsync(
            reservation,
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            reservation.UserId,
            "Reservation Request Submitted",
            $"Your reservation {reservation.ReservationNumber} has been submitted successfully and is waiting for confirmation.",
            "Reservation",
            reservation.Id.ToString(),
            cancellationToken: cancellationToken);

        if (parking.OwnerId != Guid.Empty &&
            parking.OwnerId != reservation.UserId)
        {
            await _notificationDispatcher.DispatchAsync(
                parking.OwnerId,
                "New Reservation Request",
                $"A new reservation request {reservation.ReservationNumber} has been received for your parking.",
                "Reservation",
                reservation.Id.ToString(),
                cancellationToken: cancellationToken);
        }
        await InvalidateAvailabilityCacheAsync(
             cancellationToken);

        return MapToResponse(reservation);
    }

    public async Task<ReservationResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(
            id,
            cancellationToken);

        return reservation is null
            ? null
            : MapToResponse(reservation);
    }

    public async Task<PagedResult<ReservationResponse>> GetAllAsync(
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _reservationRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<ReservationResponse>>
        GetMyReservationsAsync(
            Guid userId,
            ReservationQueryParameters parameters,
            CancellationToken cancellationToken = default)
    {
        var result = await _reservationRepository.GetPagedByUserIdAsync(
            userId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<ReservationResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var parking = await _parkingRepository.GetByIdAsync(
            parkingId,
            cancellationToken);

        if (parking is null)
        {
            throw new KeyNotFoundException(
                "Parking was not found.");
        }

        var result = await _reservationRepository.GetPagedByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<ReservationResponse?> UpdateAsync(
        Guid id,
        Guid userId,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservation is null || reservation.UserId != userId)
        {
            return null;
        }

        if (reservation.Status != "Pending")
        {
            throw new InvalidOperationException(
                "Only pending reservations can be updated.");
        }

        var now = DateTimeOffset.UtcNow;

        if (request.StartTime <= now)
        {
            throw new ArgumentException(
                "Reservation start time must be in the future.");
        }

        if (request.EndTime <= request.StartTime)
        {
            throw new ArgumentException(
                "Reservation end time must be after its start time.");
        }

        var hasOverlap =
            await _reservationRepository.HasOverlappingReservationAsync(
                reservation.ParkingId,
                request.StartTime,
                request.EndTime,
                reservation.Id,
                cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "The parking has an overlapping reservation.");
        }

        reservation.StartTime = request.StartTime;
        reservation.EndTime = request.EndTime;
        reservation.UpdatedAt = now;

        await _reservationRepository.UpdateAsync(
            reservation,
            cancellationToken);
        var parking = await _parkingRepository.GetByIdAsync(
        reservation.ParkingId,
        cancellationToken);

        if (parking is not null &&
            parking.OwnerId != Guid.Empty &&
            parking.OwnerId != reservation.UserId)
        {
            await _notificationDispatcher.DispatchAsync(
                parking.OwnerId,
                "Reservation Updated",
                $"Reservation {reservation.ReservationNumber} has been updated by the customer.",
                "Reservation",
                reservation.Id.ToString(),
                cancellationToken: cancellationToken);
        }
        await InvalidateAvailabilityCacheAsync(
            cancellationToken);

        return MapToResponse(reservation);
    }

    public async Task<bool> CancelAsync(
    Guid id,
    Guid userId,
    CancelReservationRequest request,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException(
                "Cancellation reason is required.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservation is null || reservation.UserId != userId)
        {
            return false;
        }

        if (reservation.Status != "Pending" &&
            reservation.Status != "Confirmed")
        {
            throw new InvalidOperationException(
                "Only pending or confirmed reservations can be cancelled.");
        }

        var now = DateTimeOffset.UtcNow;

        reservation.Status = "Cancelled";
        reservation.CancelledAt = now;
        reservation.CancellationReason = request.Reason.Trim();
        reservation.UpdatedAt = now;

        await _reservationRepository.UpdateAsync(
            reservation,
            cancellationToken);

        var parking = await _parkingRepository.GetByIdAsync(
            reservation.ParkingId,
            cancellationToken);

        if (parking is not null &&
            parking.OwnerId != Guid.Empty &&
            parking.OwnerId != reservation.UserId)
        {
            await _notificationDispatcher.DispatchAsync(
                parking.OwnerId,
                "Reservation Cancelled",
                $"Reservation {reservation.ReservationNumber} has been cancelled by the customer.",
                "Reservation",
                reservation.Id.ToString(),
                cancellationToken: cancellationToken);
        }
        await InvalidateAvailabilityCacheAsync(
           cancellationToken);

        return true;
    }
    public async Task<ReservationResponse?> ConfirmAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservation is null)
        {
            return null;
        }

        if (reservation.Status != "Pending")
        {
            throw new InvalidOperationException(
                "Only pending reservations can be confirmed.");
        }

        var now = DateTimeOffset.UtcNow;

        reservation.Status = "Confirmed";
        reservation.ConfirmedAt = now;
        reservation.UpdatedAt = now;

        await _reservationRepository.UpdateAsync(
            reservation,
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            reservation.UserId,
            "Reservation Confirmed",
            $"Your reservation {reservation.ReservationNumber} has been confirmed.",
            "Reservation",
            reservation.Id.ToString(),
            cancellationToken: cancellationToken);

        await InvalidateAvailabilityCacheAsync(
            cancellationToken);

        return MapToResponse(reservation);
    }

    public async Task<ReservationResponse?> MarkArrivalAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservation is null)
        {
            return null;
        }

        if (reservation.Status != "Confirmed")
        {
            throw new InvalidOperationException(
                "Only confirmed reservations can be marked as arrived.");
        }

        var now = DateTimeOffset.UtcNow;

        reservation.Status = "Active";
        reservation.ActualArrivalTime = now;
        reservation.UpdatedAt = now;

        await _reservationRepository.UpdateAsync(
            reservation,
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            reservation.UserId,
            "Arrival Registered",
            $"Your arrival for reservation {reservation.ReservationNumber} has been registered successfully.",
            "Reservation",
            reservation.Id.ToString(),
            cancellationToken: cancellationToken);

        await InvalidateAvailabilityCacheAsync(
           cancellationToken);

        return MapToResponse(reservation);
    }

    public async Task<ReservationResponse?> MarkDepartureAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservation is null)
        {
            return null;
        }

        if (reservation.Status != "Active")
        {
            throw new InvalidOperationException(
                "Only active reservations can be marked as departed.");
        }

        var now = DateTimeOffset.UtcNow;

        if (reservation.ActualArrivalTime.HasValue &&
            now <= reservation.ActualArrivalTime.Value)
        {
            throw new InvalidOperationException(
                "Departure time must be after arrival time.");
        }

        reservation.Status = "Completed";
        reservation.ActualDepartureTime = now;
        reservation.UpdatedAt = now;

        await _reservationRepository.UpdateAsync(
            reservation,
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            reservation.UserId,
            "Reservation Completed",
            $"Your reservation {reservation.ReservationNumber} has been completed successfully.",
            "Reservation",
            reservation.Id.ToString(),
            cancellationToken: cancellationToken);

        await InvalidateAvailabilityCacheAsync(
            cancellationToken);

        return MapToResponse(reservation);
    }

    private async Task<string> GenerateReservationNumberAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"PRK-{Guid.NewGuid():N}"
                .Substring(0, 12)
                .ToUpperInvariant();

            var exists =
                await _reservationRepository.ExistsByReservationNumberAsync(
                    number,
                    cancellationToken);

            if (!exists)
            {
                return number;
            }
        }

        throw new InvalidOperationException(
            "Could not generate a unique reservation number.");
    }

    private PagedResult<ReservationResponse> MapToPagedResponse(
        PagedResult<Reservation> result)
    {
        return new PagedResult<ReservationResponse>
        {
            Items = _mapper.Map<List<ReservationResponse>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private ReservationResponse MapToResponse(
        Reservation reservation)
    {
        return _mapper.Map<ReservationResponse>(reservation);
    }

    private async Task InvalidateAvailabilityCacheAsync(
    CancellationToken cancellationToken)
    {
        await _cacheService.RemoveByPrefixAsync(
            CacheKeys.ParkingAvailabilityPrefix(),
            cancellationToken);
    }
}
