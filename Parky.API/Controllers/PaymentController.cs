
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Payment;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PaymentResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentResponse>>>>
        GetAll(
            [FromQuery] PaymentQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<PaymentResponse>>.SuccessResponse(
                payments,
                "Payments retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetByIdAsync(
            id,
            cancellationToken);

        if (payment is null)
        {
            return NotFound(
                ApiResponse<PaymentResponse>.FailureResponse(
                    "Payment was not found."));
        }

        return Ok(
            ApiResponse<PaymentResponse>.SuccessResponse(
                payment,
                "Payment retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<PaymentResponse>.SuccessResponse(
            payment,
            "Payment created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = payment.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Update(
        Guid id,
        [FromBody] UpdatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (payment is null)
        {
            return NotFound(
                ApiResponse<PaymentResponse>.FailureResponse(
                    "Payment was not found."));
        }

        return Ok(
            ApiResponse<PaymentResponse>.SuccessResponse(
                payment,
                "Payment updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _paymentService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Payment was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Payment deleted successfully."));
    }

    [HttpGet("by-reservation/{reservationId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PaymentResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PaymentResponse>>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentResponse>>>>
        GetByReservation(
            Guid reservationId,
            [FromQuery] PaymentQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetByReservationIdAsync(
            reservationId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<PaymentResponse>>.SuccessResponse(
                payments,
                "Reservation payments retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.UpdateStatusAsync(
            id,
            request,
            cancellationToken);

        if (payment is null)
        {
            return NotFound(
                ApiResponse<PaymentResponse>.FailureResponse(
                    "Payment was not found."));
        }

        return Ok(
            ApiResponse<PaymentResponse>.SuccessResponse(
                payment,
                "Payment status updated successfully."));
    }
}
