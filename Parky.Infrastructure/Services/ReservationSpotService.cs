
using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ReservationSpot;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public class ReservationSpotService : IReservationSpotService
{
    private readonly IReservationSpotRepository _reservationSpotRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IMapper _mapper;

    public ReservationSpotService(
        IReservationSpotRepository reservationSpotRepository,
        IReservationRepository reservationRepository,
        IParkingSpotRepository parkingSpotRepository,
        IMapper mapper)
    {
        _reservationSpotRepository = reservationSpotRepository;
        _reservationRepository = reservationRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _mapper = mapper;
    }

    public async Task<ReservationSpotResponse> CreateAsync(
        CreateReservationSpotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReservationId == Guid.Empty ||
            request.ParkingSpotId == Guid.Empty)
        {
            throw new ArgumentException(
                "ReservationId and ParkingSpotId are required.");
        }

        if (request.PricePerHour <= 0)
        {
            throw new ArgumentException(
                "PricePerHour must be greater than zero.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException(
                "Reservation was not found.");
        }

        if (reservation.Status != "Pending")
        {
            throw new InvalidOperationException(
                "Spots can only be assigned to pending reservations.");
        }

        if (reservation.EndTime <= reservation.StartTime)
        {
            throw new InvalidOperationException(
                "The reservation time range is invalid.");
        }

        var parkingSpot = await _parkingSpotRepository.GetByIdAsync(
            request.ParkingSpotId,
            cancellationToken);

        if (parkingSpot is null)
        {
            throw new KeyNotFoundException(
                "Parking spot was not found.");
        }

        var parkingFloor = parkingSpot.ParkingFloor;

        if (parkingFloor is null)
        {
            throw new InvalidOperationException(
                "The parking spot is not associated with a parking floor.");
        }

        if (parkingFloor.ParkingId != reservation.ParkingId)
        {
            throw new InvalidOperationException(
                "The parking spot must belong to the reservation parking.");
        }

        var alreadyAssigned = await _reservationSpotRepository.ExistsAsync(
            request.ReservationId,
            request.ParkingSpotId,
            cancellationToken);

        if (alreadyAssigned)
        {
            throw new InvalidOperationException(
                "This parking spot is already assigned to this reservation.");
        }

        var spotAssignments =
            await _reservationSpotRepository.GetByParkingSpotIdAsync(
                request.ParkingSpotId,
                cancellationToken);

        foreach (var assignment in spotAssignments)
        {
            var existingReservation =
                await _reservationRepository.GetByIdAsync(
                    assignment.ReservationId,
                    cancellationToken);

            if (existingReservation is null)
            {
                continue;
            }

            if (existingReservation.Status is "Cancelled" or "Completed")
            {
                continue;
            }

            var overlaps =
                reservation.StartTime < existingReservation.EndTime &&
                existingReservation.StartTime < reservation.EndTime;

            if (overlaps)
            {
                throw new InvalidOperationException(
                    "This parking spot is already reserved during the selected time.");
            }
        }

        var durationInHours =
            (decimal)(reservation.EndTime - reservation.StartTime).TotalHours;

        var reservedAmount = Math.Round(
            durationInHours * request.PricePerHour,
            2,
            MidpointRounding.AwayFromZero);

        var reservationSpot = new ReservationSpot
        {
            Id = Guid.NewGuid(),
            ReservationId = request.ReservationId,
            ParkingSpotId = request.ParkingSpotId,
            PricePerHour = request.PricePerHour,
            ReservedAmount = reservedAmount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _reservationSpotRepository.AddAsync(
            reservationSpot,
            cancellationToken);

        return MapToResponse(reservationSpot);
    }

    public async Task<ReservationSpotResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var reservationSpot = await _reservationSpotRepository.GetByIdAsync(
            id,
            cancellationToken);

        return reservationSpot is null
            ? null
            : MapToResponse(reservationSpot);
    }

    public async Task<PagedResult<ReservationSpotResponse>> GetAllAsync(
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _reservationSpotRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<ReservationSpotResponse>> GetByReservationIdAsync(
        Guid reservationId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "ReservationId is required.");
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result =
            await _reservationSpotRepository.GetPagedByReservationIdAsync(
                reservationId,
                parameters,
                cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<ReservationSpotResponse>> GetByParkingSpotIdAsync(
        Guid parkingSpotId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parkingSpotId == Guid.Empty)
        {
            throw new ArgumentException(
                "ParkingSpotId is required.");
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result =
            await _reservationSpotRepository.GetPagedByParkingSpotIdAsync(
                parkingSpotId,
                parameters,
                cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<ReservationSpotResponse?> UpdateAsync(
        Guid id,
        UpdateReservationSpotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PricePerHour <= 0)
        {
            throw new ArgumentException(
                "PricePerHour must be greater than zero.");
        }

        var reservationSpot = await _reservationSpotRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservationSpot is null)
        {
            return null;
        }

        var reservation = await _reservationRepository.GetByIdAsync(
            reservationSpot.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException(
                "Reservation was not found.");
        }

        if (reservation.Status != "Pending")
        {
            throw new InvalidOperationException(
                "A spot assignment can only be updated for pending reservations.");
        }

        var durationInHours =
            (decimal)(reservation.EndTime - reservation.StartTime).TotalHours;

        if (durationInHours <= 0)
        {
            throw new InvalidOperationException(
                "The reservation time range is invalid.");
        }

        reservationSpot.PricePerHour = request.PricePerHour;

        reservationSpot.ReservedAmount = Math.Round(
            durationInHours * request.PricePerHour,
            2,
            MidpointRounding.AwayFromZero);

        await _reservationSpotRepository.UpdateAsync(
            reservationSpot,
            cancellationToken);

        return MapToResponse(reservationSpot);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var reservationSpot = await _reservationSpotRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (reservationSpot is null)
        {
            return false;
        }

        var reservation = await _reservationRepository.GetByIdAsync(
            reservationSpot.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException(
                "Reservation was not found.");
        }

        if (reservation.Status != "Pending")
        {
            throw new InvalidOperationException(
                "A spot assignment can only be removed from a pending reservation.");
        }

        await _reservationSpotRepository.DeleteAsync(
            reservationSpot,
            cancellationToken);

        return true;
    }

    private static void ValidateQueryParameters(
        ReservationSpotQueryParameters parameters)
    {
        if (parameters.ReservationId.HasValue &&
            parameters.ReservationId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "ReservationId is invalid.");
        }

        if (parameters.ParkingSpotId.HasValue &&
            parameters.ParkingSpotId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "ParkingSpotId is invalid.");
        }

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.ReservationStatus?.Length > 50)
        {
            throw new ArgumentException(
                "ReservationStatus cannot exceed 50 characters.");
        }

        if (parameters.MinPricePerHour.HasValue &&
            parameters.MinPricePerHour.Value < 0)
        {
            throw new ArgumentException(
                "MinPricePerHour cannot be negative.");
        }

        if (parameters.MaxPricePerHour.HasValue &&
            parameters.MaxPricePerHour.Value < 0)
        {
            throw new ArgumentException(
                "MaxPricePerHour cannot be negative.");
        }

        if (parameters.MinPricePerHour.HasValue &&
            parameters.MaxPricePerHour.HasValue &&
            parameters.MinPricePerHour.Value >
            parameters.MaxPricePerHour.Value)
        {
            throw new ArgumentException(
                "MinPricePerHour cannot be greater than MaxPricePerHour.");
        }

        if (parameters.MinReservedAmount.HasValue &&
            parameters.MinReservedAmount.Value < 0)
        {
            throw new ArgumentException(
                "MinReservedAmount cannot be negative.");
        }

        if (parameters.MaxReservedAmount.HasValue &&
            parameters.MaxReservedAmount.Value < 0)
        {
            throw new ArgumentException(
                "MaxReservedAmount cannot be negative.");
        }

        if (parameters.MinReservedAmount.HasValue &&
            parameters.MaxReservedAmount.HasValue &&
            parameters.MinReservedAmount.Value >
            parameters.MaxReservedAmount.Value)
        {
            throw new ArgumentException(
                "MinReservedAmount cannot be greater than MaxReservedAmount.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }
    }

    private PagedResult<ReservationSpotResponse> MapToPagedResponse(
        PagedResult<ReservationSpot> result)
    {
        return new PagedResult<ReservationSpotResponse>
        {
            Items = _mapper.Map<List<ReservationSpotResponse>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private ReservationSpotResponse MapToResponse(
        ReservationSpot reservationSpot)
    {
        return _mapper.Map<ReservationSpotResponse>(reservationSpot);
    }
}
