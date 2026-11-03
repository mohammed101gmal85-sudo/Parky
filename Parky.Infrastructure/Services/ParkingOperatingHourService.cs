using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingOperatingHour;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public sealed class ParkingOperatingHourService
    : IParkingOperatingHourService
{
    private readonly IParkingOperatingHourRepository _repository;
    private readonly IMapper _mapper;

    public ParkingOperatingHourService(
        IParkingOperatingHourRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create Parking Operating Hour
    // ----------------------------------------------------

    public async Task<ParkingOperatingHourResponse> CreateAsync(
        CreateParkingOperatingHourRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateDay(request.DayOfWeek);

        ValidateSchedule(
            request.OpenTime,
            request.CloseTime,
            request.IsClosed,
            request.IsOpen24Hours);

        if (request.ParkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID is required.");
        }

        if (!await _repository.ParkingExistsAsync(
                request.ParkingId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                "Parking was not found.");
        }

        if (!await _repository.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking.");
        }

        if (await _repository.DayExistsAsync(
                request.ParkingId,
                request.DayOfWeek,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "Operating hours already exist for this day.");
        }

        var operatingHour = new ParkingOperatingHour
        {
            ParkingId = request.ParkingId,
            DayOfWeek = request.DayOfWeek,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime,
            IsClosed = request.IsClosed,
            IsOpen24Hours = request.IsOpen24Hours,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(
            operatingHour,
            cancellationToken);

        return MapToResponse(operatingHour);
    }

    // ----------------------------------------------------
    // Get Parking Operating Hour By Id
    // ----------------------------------------------------

    public async Task<ParkingOperatingHourResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var operatingHour = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        return operatingHour is null
            ? null
            : MapToResponse(operatingHour);
    }

    // ----------------------------------------------------
    // Get All Parking Operating Hours
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingOperatingHourResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var operatingHours = await _repository.GetAllAsync(
            cancellationToken);

        return _mapper.Map<List<ParkingOperatingHourResponse>>(
            operatingHours);
    }

    // ----------------------------------------------------
    // Get All Parking Operating Hours With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingOperatingHourResponse>> GetAllAsync(
        ParkingOperatingHourQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _repository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Parking Operating Hours By Parking Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingOperatingHourResponse>>
        GetByParkingIdAsync(
            Guid parkingId,
            CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.",
                nameof(parkingId));
        }

        var operatingHours = await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        return _mapper.Map<List<ParkingOperatingHourResponse>>(
            operatingHours);
    }

    // ----------------------------------------------------
    // Update Parking Operating Hour
    // ----------------------------------------------------

    public async Task<ParkingOperatingHourResponse?> UpdateAsync(
        Guid id,
        UpdateParkingOperatingHourRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateDay(request.DayOfWeek);

        ValidateSchedule(
            request.OpenTime,
            request.CloseTime,
            request.IsClosed,
            request.IsOpen24Hours);

        if (request.ParkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID is required.");
        }

        var operatingHour = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (operatingHour is null)
        {
            return null;
        }

        if (!await _repository.ParkingBelongsToOwnerAsync(
                operatingHour.ParkingId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking.");
        }

        if (operatingHour.ParkingId != request.ParkingId)
        {
            if (!await _repository.ParkingExistsAsync(
                    request.ParkingId,
                    cancellationToken))
            {
                throw new KeyNotFoundException(
                    "Target parking was not found.");
            }

            if (!await _repository.ParkingBelongsToOwnerAsync(
                    request.ParkingId,
                    ownerId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "You are not allowed to manage the target parking.");
            }
        }

        if (await _repository.DayExistsAsync(
                request.ParkingId,
                request.DayOfWeek,
                id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Operating hours already exist for this day.");
        }

        operatingHour.ParkingId = request.ParkingId;
        operatingHour.DayOfWeek = request.DayOfWeek;
        operatingHour.OpenTime = request.OpenTime;
        operatingHour.CloseTime = request.CloseTime;
        operatingHour.IsClosed = request.IsClosed;
        operatingHour.IsOpen24Hours = request.IsOpen24Hours;
        operatingHour.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            operatingHour,
            cancellationToken);

        return MapToResponse(operatingHour);
    }

    // ----------------------------------------------------
    // Delete Parking Operating Hour
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID is required.",
                nameof(ownerId));
        }

        var operatingHour = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (operatingHour is null)
        {
            return false;
        }

        if (!await _repository.ParkingBelongsToOwnerAsync(
                operatingHour.ParkingId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking.");
        }

        await _repository.DeleteAsync(
            operatingHour,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Update Parking Operating Hour Status
    // ----------------------------------------------------

    public async Task<ParkingOperatingHourResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingOperatingHourStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID is required.",
                nameof(ownerId));
        }

        if (request.IsClosed && request.IsOpen24Hours)
        {
            throw new ArgumentException(
                "A parking day cannot be closed and open 24 hours at the same time.");
        }

        var operatingHour = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (operatingHour is null)
        {
            return null;
        }

        if (!await _repository.ParkingBelongsToOwnerAsync(
                operatingHour.ParkingId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking.");
        }

        if (!request.IsClosed &&
            !request.IsOpen24Hours &&
            operatingHour.OpenTime >= operatingHour.CloseTime)
        {
            throw new InvalidOperationException(
                "The existing opening time must be earlier than the closing time before normal hours can be enabled.");
        }

        operatingHour.IsClosed = request.IsClosed;
        operatingHour.IsOpen24Hours = request.IsOpen24Hours;
        operatingHour.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            operatingHour,
            cancellationToken);

        return MapToResponse(operatingHour);
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        ParkingOperatingHourQueryParameters parameters)
    {
        if (parameters.ParkingId.HasValue &&
            parameters.ParkingId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is invalid.");
        }

        if (parameters.DayOfWeek.HasValue &&
            !Enum.IsDefined(
                typeof(DayOfWeek),
                parameters.DayOfWeek.Value))
        {
            throw new ArgumentException(
                "DayOfWeek must be a valid day of the week.");
        }

        if (parameters.OpenTimeFrom.HasValue &&
            parameters.OpenTimeTo.HasValue &&
            parameters.OpenTimeFrom.Value > parameters.OpenTimeTo.Value)
        {
            throw new ArgumentException(
                "OpenTimeFrom cannot be later than OpenTimeTo.");
        }

        if (parameters.CloseTimeFrom.HasValue &&
            parameters.CloseTimeTo.HasValue &&
            parameters.CloseTimeFrom.Value > parameters.CloseTimeTo.Value)
        {
            throw new ArgumentException(
                "CloseTimeFrom cannot be later than CloseTimeTo.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }

        if (parameters.UpdatedFrom.HasValue &&
            parameters.UpdatedTo.HasValue &&
            parameters.UpdatedFrom.Value > parameters.UpdatedTo.Value)
        {
            throw new ArgumentException(
                "UpdatedFrom cannot be later than UpdatedTo.");
        }
    }

    // ----------------------------------------------------
    // Validate Day
    // ----------------------------------------------------

    private static void ValidateDay(DayOfWeek dayOfWeek)
    {
        if (!Enum.IsDefined(typeof(DayOfWeek), dayOfWeek))
        {
            throw new ArgumentException(
                "DayOfWeek must be a valid day of the week.");
        }
    }

    // ----------------------------------------------------
    // Validate Operating Schedule
    // ----------------------------------------------------

    private static void ValidateSchedule(
        TimeOnly openTime,
        TimeOnly closeTime,
        bool isClosed,
        bool isOpen24Hours)
    {
        if (isClosed && isOpen24Hours)
        {
            throw new ArgumentException(
                "A parking day cannot be closed and open 24 hours at the same time.");
        }

        if (!isClosed &&
            !isOpen24Hours &&
            openTime >= closeTime)
        {
            throw new ArgumentException(
                "OpenTime must be earlier than CloseTime for normal operating hours.");
        }
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private ParkingOperatingHourResponse MapToResponse(
        ParkingOperatingHour operatingHour)
    {
        return _mapper.Map<ParkingOperatingHourResponse>(
            operatingHour);
    }

    private PagedResult<ParkingOperatingHourResponse> MapToPagedResponse(
        PagedResult<ParkingOperatingHour> result)
    {
        return new PagedResult<ParkingOperatingHourResponse>
        {
            Items = _mapper.Map<List<ParkingOperatingHourResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}