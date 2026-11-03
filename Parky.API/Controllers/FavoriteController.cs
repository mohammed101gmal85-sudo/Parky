using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Favorite;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FavoriteController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoriteController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FavoriteResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<FavoriteResponse>>>> GetAll(
        [FromQuery] FavoriteQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var favorites = await _favoriteService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<FavoriteResponse>>.SuccessResponse(
                favorites,
                "Favorites retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FavoriteResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var favorite = await _favoriteService.GetByIdAsync(
            id,
            cancellationToken);

        if (favorite is null)
        {
            return NotFound(
                ApiResponse<FavoriteResponse>.FailureResponse(
                    "Favorite was not found."));
        }

        return Ok(
            ApiResponse<FavoriteResponse>.SuccessResponse(
                favorite,
                "Favorite retrieved successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FavoriteResponse>>> Add(
        [FromBody] CreateFavoriteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<FavoriteResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var favorite = await _favoriteService.AddAsync(
            request,
            userId.Value,
            cancellationToken);

        var response = ApiResponse<FavoriteResponse>.SuccessResponse(
            favorite,
            "Parking added to favorites successfully.");

        return Created("/api/Favorite/my-favorites", response);
    }

    [HttpGet("my-favorites")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FavoriteResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<FavoriteResponse>>>> GetMyFavorites(
        [FromQuery] FavoriteQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<PagedResult<FavoriteResponse>>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var favorites = await _favoriteService.GetMyFavoritesAsync(
            userId.Value,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<FavoriteResponse>>.SuccessResponse(
                favorites,
                "Your favorites retrieved successfully."));
    }

    [HttpGet("is-favorite/{parkingId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> IsFavorite(
        Guid parkingId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _favoriteService.IsFavoriteAsync(
            parkingId,
            userId.Value,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                result,
                "Favorite status retrieved successfully."));
    }

    [HttpDelete("by-parking/{parkingId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> Remove(
        Guid parkingId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        await _favoriteService.RemoveAsync(
            parkingId,
            userId.Value,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking removed from favorites successfully."));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }
}