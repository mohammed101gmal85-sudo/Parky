using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Parky.API.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Route = "/hubs/notifications";

    public const string NotificationReceivedEvent =
        "NotificationReceived";

    public static string GetUserGroupName(Guid userId)
    {
        return $"user:{userId:N}";
    }

    public override async Task OnConnectedAsync()
    {
        var userIdValue =
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId) ||
            userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetUserGroupName(userId));

        await base.OnConnectedAsync();
    }

    public const string NotificationUpdatedEvent =
    "NotificationUpdated";

    public const string NotificationDeletedEvent =
        "NotificationDeleted";

    public const string NotificationsReadAllEvent =
        "NotificationsReadAll";

    public const string UnreadCountUpdatedEvent =
        "UnreadCountUpdated";
}