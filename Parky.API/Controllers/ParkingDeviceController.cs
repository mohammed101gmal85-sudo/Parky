using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingDevice;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class ParkingDeviceController : ControllerBase
{
    private readonly IParkingDeviceService _deviceService;

    public ParkingDeviceController(IParkingDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingDeviceResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingDeviceResponse>>>> GetAll(
        [FromQuery] ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var devices = await _deviceService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingDeviceResponse>>.SuccessResponse(
                devices,
                "Parking devices retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingDeviceResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.GetByIdAsync(
            id,
            cancellationToken);

        if (device is null)
        {
            return NotFound(
                ApiResponse<ParkingDeviceResponse>.FailureResponse(
                    "Parking device was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingDeviceResponse>.SuccessResponse(
                device,
                "Parking device retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ParkingDeviceResponse>>> Create(
        [FromBody] CreateParkingDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<ParkingDeviceResponse>.SuccessResponse(
            device,
            "Parking device created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = device.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingDeviceResponse>>> Update(
        Guid id,
        [FromBody] UpdateParkingDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (device is null)
        {
            return NotFound(
                ApiResponse<ParkingDeviceResponse>.FailureResponse(
                    "Parking device was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingDeviceResponse>.SuccessResponse(
                device,
                "Parking device updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deactivated = await _deviceService.DeactivateAsync(
            id,
            cancellationToken);

        if (!deactivated)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking device was not found or is already inactive."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking device deactivated successfully."));
    }

    [HttpGet("by-gate/{parkingGateId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingDeviceResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingDeviceResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingDeviceResponse>>>> GetByParkingGate(
        Guid parkingGateId,
        [FromQuery] ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var devices = await _deviceService.GetByParkingGateIdAsync(
            parkingGateId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingDeviceResponse>>.SuccessResponse(
                devices,
                "Parking gate devices retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParkingDeviceResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingDeviceResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateParkingDeviceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.UpdateStatusAsync(
            id,
            request,
            cancellationToken);

        if (device is null)
        {
            return NotFound(
                ApiResponse<ParkingDeviceResponse>.FailureResponse(
                    "Parking device was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingDeviceResponse>.SuccessResponse(
                device,
                "Parking device status updated successfully."));
    }
}