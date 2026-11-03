using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Invoice;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<InvoiceResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceResponse>>>>
        GetAll(
            [FromQuery] InvoiceQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<InvoiceResponse>>.SuccessResponse(
                invoices,
                "Invoices retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(
            id,
            cancellationToken);

        if (invoice is null)
        {
            return NotFound(
                ApiResponse<InvoiceResponse>.FailureResponse(
                    "Invoice was not found."));
        }

        return Ok(
            ApiResponse<InvoiceResponse>.SuccessResponse(
                invoice,
                "Invoice retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<InvoiceResponse>.SuccessResponse(
            invoice,
            "Invoice created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = invoice.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Update(
        Guid id,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(
            ApiResponse<InvoiceResponse>.SuccessResponse(
                invoice,
                "Invoice updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _invoiceService.DeleteAsync(
            id,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Invoice deleted successfully."));
    }

    [HttpGet("by-reservation/{reservationId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<InvoiceResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<InvoiceResponse>>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<InvoiceResponse>>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceResponse>>>>
        GetByReservation(
            Guid reservationId,
            CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetByReservationIdAsync(
            reservationId,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<InvoiceResponse>>.SuccessResponse(
                invoices,
                "Reservation invoices retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<InvoiceResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.UpdateStatusAsync(
            id,
            request,
            cancellationToken);

        return Ok(
            ApiResponse<InvoiceResponse>.SuccessResponse(
                invoice,
                "Invoice status updated successfully."));
    }
}