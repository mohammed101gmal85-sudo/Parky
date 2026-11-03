using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.QRCode;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public sealed class QRCodeController : ControllerBase
{
    private readonly IQRCodeService _qrCodeService;

    public QRCodeController(IQRCodeService qrCodeService)
    {
        _qrCodeService = qrCodeService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreatedQRCodeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreatedQRCodeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreatedQRCodeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CreatedQRCodeResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CreatedQRCodeResponse>>> Create(
        [FromBody] CreateQRCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _qrCodeService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<CreatedQRCodeResponse>.SuccessResponse(
            result,
            "QR code created successfully.");

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<QRCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QRCodeResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QRCodeResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _qrCodeService.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<QRCodeResponse>.FailureResponse(
                    "QR code was not found."));
        }

        return Ok(
            ApiResponse<QRCodeResponse>.SuccessResponse(
                result,
                "QR code retrieved successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var request = new RevokeQRCodeRequest
        {
            Reason = "Revoked by the parking owner."
        };

        var revoked = await _qrCodeService.RevokeAsync(
            id,
            request,
            cancellationToken);

        if (!revoked)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "QR code was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "QR code revoked successfully."));
    }

    [HttpGet("reservation/{reservationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QRCodeResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QRCodeResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<QRCodeResponse>>>> GetByReservation(
        Guid reservationId,
        [FromQuery] QRCodeQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _qrCodeService.GetPagedByReservationIdAsync(
            reservationId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<QRCodeResponse>>.SuccessResponse(
                result,
                "Reservation QR codes retrieved successfully."));
    }

    [HttpPost("validate-and-use")]
    [ProducesResponseType(typeof(ApiResponse<QRCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QRCodeResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<QRCodeResponse>>> ValidateAndUse(
        [FromBody] ValidateQRCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _qrCodeService.ValidateAndUseAsync(
            request,
            cancellationToken);

        if (result is null)
        {
            return BadRequest(
                ApiResponse<QRCodeResponse>.FailureResponse(
                    "QR code is invalid, expired, revoked, or already used."));
        }

        return Ok(
            ApiResponse<QRCodeResponse>.SuccessResponse(
                result,
                "QR code validated and used successfully."));
    }

    [HttpPatch("{id:guid}/revoke")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> Revoke(
        Guid id,
        [FromBody] RevokeQRCodeRequest request,
        CancellationToken cancellationToken)
    {
        var revoked = await _qrCodeService.RevokeAsync(
            id,
            request,
            cancellationToken);

        if (!revoked)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "QR code was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "QR code revoked successfully."));
    }
}