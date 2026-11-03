using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingImage;
using Parky.Application.Interfaces.Services;
using Parky.API.Models.ParkingImage;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingImageController : ControllerBase
{
    private readonly IParkingImageService _service;

    public ParkingImageController(IParkingImageService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingImageResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingImageResponse>>>> GetAll(
        [FromQuery] ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var images = await _service.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingImageResponse>>.SuccessResponse(
                images,
                "Parking images retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingImageResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var image = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (image is null)
        {
            return NotFound(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Parking image was not found."));
        }

        return Ok(
            ApiResponse<ParkingImageResponse>.SuccessResponse(
                image,
                "Parking image retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingImageResponse>>> Create(
        [FromForm] CreateParkingImageForm request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        if (request.Image is null)
        {
            return BadRequest(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Image file is required."));
        }

        await using var imageStream =
            request.Image.OpenReadStream();

        var serviceRequest = new CreateParkingImageRequest
        {
            ParkingId = request.ParkingId,
            ImageContent = imageStream,
            ContentType = request.Image.ContentType,
            AltText = request.AltText,
            IsPrimary = request.IsPrimary,
            DisplayOrder = request.DisplayOrder
        };

        var image = await _service.CreateAsync(
            serviceRequest,
            ownerId,
            cancellationToken);

        var response = ApiResponse<ParkingImageResponse>.SuccessResponse(
            image,
            "Parking image uploaded successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = image.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingImageResponse>>> Update(
        Guid id,
        [FromForm] UpdateParkingImageForm request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        if (request.Image is null)
        {
            return BadRequest(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Image file is required."));
        }

        await using var imageStream =
            request.Image.OpenReadStream();

        var serviceRequest = new UpdateParkingImageRequest
        {
            ParkingId = request.ParkingId,
            ImageContent = imageStream,
            ContentType = request.Image.ContentType,
            AltText = request.AltText,
            IsPrimary = request.IsPrimary,
            DisplayOrder = request.DisplayOrder
        };

        var image = await _service.UpdateAsync(
            id,
            serviceRequest,
            ownerId,
            cancellationToken);

        if (image is null)
        {
            return NotFound(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Parking image was not found."));
        }

        return Ok(
            ApiResponse<ParkingImageResponse>.SuccessResponse(
                image,
                "Parking image updated successfully."));
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
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var deleted = await _service.DeleteAsync(
            id,
            ownerId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking image was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking image deleted successfully."));
    }

    [HttpGet("by-parking/{parkingId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingImageResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingImageResponse>>>> GetByParkingId(
        Guid parkingId,
        [FromQuery] ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var images = await _service.GetByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingImageResponse>>.SuccessResponse(
                images,
                "Parking images retrieved successfully."));
    }

    [HttpPatch("{id:guid}/primary")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingImageResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingImageResponse>>> UpdatePrimaryStatus(
        Guid id,
        [FromBody] UpdateParkingImagePrimaryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var image = await _service.UpdatePrimaryStatusAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (image is null)
        {
            return NotFound(
                ApiResponse<ParkingImageResponse>.FailureResponse(
                    "Parking image was not found."));
        }

        return Ok(
            ApiResponse<ParkingImageResponse>.SuccessResponse(
                image,
                "Parking image primary status updated successfully."));
    }

    [HttpPut("by-parking/{parkingId:guid}/reorder")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<ParkingImageResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParkingImageResponse>>>> Reorder(
        Guid parkingId,
        [FromBody] List<Guid> imageIds,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<IReadOnlyList<ParkingImageResponse>>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var images = await _service.ReorderAsync(
            parkingId,
            imageIds,
            ownerId,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<ParkingImageResponse>>.SuccessResponse(
                images,
                "Parking images reordered successfully."));
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var ownerIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(ownerIdValue, out ownerId);
    }
}