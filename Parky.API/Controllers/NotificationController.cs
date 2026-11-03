using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Notification;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationResponse>>>> GetAll(
        [FromQuery] NotificationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var notifications = await _notificationService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<NotificationResponse>>.SuccessResponse(
                notifications,
                "Notifications retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationService.GetByIdAsync(
            id,
            cancellationToken);

        if (notification is null)
        {
            return NotFound(
                ApiResponse<NotificationResponse>.FailureResponse(
                    "Notification was not found."));
        }

        return Ok(
            ApiResponse<NotificationResponse>.SuccessResponse(
                notification,
                "Notification retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<NotificationResponse>.SuccessResponse(
            notification,
            "Notification created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = notification.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> Update(
        Guid id,
        [FromBody] UpdateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Ok(
            ApiResponse<NotificationResponse>.SuccessResponse(
                notification,
                "Notification updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteByAdmin(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationService.GetByIdAsync(
            id,
            cancellationToken);

        if (notification is null)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Notification was not found."));
        }

        await _notificationService.DeleteAsync(
            id,
            notification.UserId,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Notification deleted successfully."));
    }

    [HttpGet("my-notifications")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationResponse>>>> GetMyNotifications(
        [FromQuery] NotificationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<PagedResult<NotificationResponse>>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var notifications = await _notificationService.GetMyNotificationsAsync(
            userId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<NotificationResponse>>.SuccessResponse(
                notifications,
                "Your notifications retrieved successfully."));
    }

    [HttpGet("my-unread")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationResponse>>>> GetMyUnreadNotifications(
        [FromQuery] NotificationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<PagedResult<NotificationResponse>>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var notifications = await _notificationService.GetMyUnreadNotificationsAsync(
            userId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<NotificationResponse>>.SuccessResponse(
                notifications,
                "Your unread notifications retrieved successfully."));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<int>>> GetMyUnreadCount(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<int>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var count = await _notificationService.GetMyUnreadCountAsync(
            userId,
            cancellationToken);

        return Ok(
            ApiResponse<int>.SuccessResponse(
                count,
                "Unread notification count retrieved successfully."));
    }

    [HttpPatch("mark-read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(
        [FromBody] MarkNotificationAsReadRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        try
        {
            await _notificationService.MarkAsReadAsync(
                request.NotificationId,
                userId,
                cancellationToken);

            return Ok(
                ApiResponse<bool>.SuccessResponse(
                    true,
                    "Notification marked as read successfully."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("mark-all-read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        await _notificationService.MarkAllAsReadAsync(
            userId,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "All notifications marked as read successfully."));
    }

    [HttpDelete("my-notifications/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteMyNotification(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        try
        {
            await _notificationService.DeleteAsync(
                id,
                userId,
                cancellationToken);

            return Ok(
                ApiResponse<bool>.SuccessResponse(
                    true,
                    "Notification deleted successfully."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out userId);
    }
}