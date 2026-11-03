
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.PricingRule;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PricingRuleController : ControllerBase
{
    private readonly IPricingRuleService _pricingRuleService;

    public PricingRuleController(
        IPricingRuleService pricingRuleService)
    {
        _pricingRuleService = pricingRuleService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PricingRuleResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PricingRuleResponse>>>>
        GetAll(
            [FromQuery] PricingRuleQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var rules = await _pricingRuleService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<PricingRuleResponse>>.SuccessResponse(
                rules,
                "Pricing rules retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PricingRuleResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var rule = await _pricingRuleService.GetByIdAsync(
            id,
            cancellationToken);

        if (rule is null)
        {
            return NotFound(
                ApiResponse<PricingRuleResponse>.FailureResponse(
                    "Pricing rule was not found."));
        }

        return Ok(
            ApiResponse<PricingRuleResponse>.SuccessResponse(
                rule,
                "Pricing rule retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PricingRuleResponse>>> Create(
        [FromBody] CreatePricingRuleRequest request,
        CancellationToken cancellationToken)
    {
        var rule = await _pricingRuleService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<PricingRuleResponse>.SuccessResponse(
            rule,
            "Pricing rule created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = rule.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<PricingRuleResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PricingRuleResponse>>> Update(
        Guid id,
        [FromBody] UpdatePricingRuleRequest request,
        CancellationToken cancellationToken)
    {
        var rule = await _pricingRuleService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (rule is null)
        {
            return NotFound(
                ApiResponse<PricingRuleResponse>.FailureResponse(
                    "Pricing rule was not found."));
        }

        return Ok(
            ApiResponse<PricingRuleResponse>.SuccessResponse(
                rule,
                "Pricing rule updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _pricingRuleService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Pricing rule was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Pricing rule deleted successfully."));
    }

    [HttpGet("by-parking/{parkingId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PricingRuleResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PricingRuleResponse>>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PricingRuleResponse>>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<PricingRuleResponse>>>>
        GetByParkingId(
            Guid parkingId,
            [FromQuery] PricingRuleQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var rules = await _pricingRuleService.GetByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<PricingRuleResponse>>.SuccessResponse(
                rules,
                "Parking pricing rules retrieved successfully."));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var updated = await _pricingRuleService.SetActiveAsync(
            id,
            true,
            cancellationToken);

        if (!updated)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Pricing rule was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Pricing rule activated successfully."));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var updated = await _pricingRuleService.SetActiveAsync(
            id,
            false,
            cancellationToken);

        if (!updated)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Pricing rule was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Pricing rule deactivated successfully."));
    }
}
