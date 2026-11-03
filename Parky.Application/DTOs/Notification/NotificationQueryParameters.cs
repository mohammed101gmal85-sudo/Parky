using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.Notification;

public sealed class NotificationQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public string? NotificationType { get; set; }

    public bool? IsRead { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? SentFrom { get; set; }

    public DateTimeOffset? SentTo { get; set; }

    public DateTimeOffset? ExpiresFrom { get; set; }

    public DateTimeOffset? ExpiresTo { get; set; }
}
