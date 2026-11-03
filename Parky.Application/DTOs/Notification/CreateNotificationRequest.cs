using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Notification;

public class CreateNotificationRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ReferenceId { get; set; }

    [StringLength(500)]
    public string? ActionUrl { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}