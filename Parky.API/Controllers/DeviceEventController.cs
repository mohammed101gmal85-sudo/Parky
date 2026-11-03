
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.DeviceEvent;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class DeviceEventController : ControllerBase
{
    private readonly IDeviceEventService _deviceEventService;

    public DeviceEventController(IDeviceEventService deviceEventService)
    {
        _deviceEventService = deviceEventService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceEventResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var events = await _deviceEventService.GetAllAsync(
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<DeviceEventResponse>>.SuccessResponse(
                events,
                "Device events retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeviceEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeviceEventResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DeviceEventResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deviceEvent = await _deviceEventService.GetByIdAsync(
            id,
            cancellationToken);

        if (deviceEvent is null)
        {
            return NotFound(
                ApiResponse<DeviceEventResponse>.FailureResponse(
                    "Device event was not found."));
        }

        return Ok(
            ApiResponse<DeviceEventResponse>.SuccessResponse(
                deviceEvent,
                "Device event retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DeviceEventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DeviceEventResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DeviceEventResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DeviceEventResponse>>> Create(
        [FromBody] CreateDeviceEventRequest request,
        CancellationToken cancellationToken)
    {
        var deviceEvent = await _deviceEventService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<DeviceEventResponse>.SuccessResponse(
            deviceEvent,
            "Device event created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = deviceEvent.Id },
            response);
    }

    [HttpGet("by-device/{parkingDeviceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceEventResponse>>>> GetByParkingDevice(
        Guid parkingDeviceId,
        CancellationToken cancellationToken)
    {
        var events = await _deviceEventService.GetByParkingDeviceIdAsync(
            parkingDeviceId,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<DeviceEventResponse>>.SuccessResponse(
                events,
                "Device events retrieved successfully."));
    }

    [HttpGet("by-type/{eventType}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceEventResponse>>>> GetByEventType(
        string eventType,
        CancellationToken cancellationToken)
    {
        var events = await _deviceEventService.GetByEventTypeAsync(
            eventType,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<DeviceEventResponse>>.SuccessResponse(
                events,
                "Device events filtered by type successfully."));
    }

    [HttpGet("by-date-range")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceEventResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceEventResponse>>>> GetByDateRange(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var events = await _deviceEventService.GetByDateRangeAsync(
            from,
            to,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<DeviceEventResponse>>.SuccessResponse(
                events,
                "Device events retrieved for the specified date range."));
    }
}
