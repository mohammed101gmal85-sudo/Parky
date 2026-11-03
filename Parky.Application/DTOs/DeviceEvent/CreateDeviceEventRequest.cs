using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.DeviceEvent;

public class CreateDeviceEventRequest
{
    [Required]
    public Guid ParkingDeviceId { get; set; }

    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EventStatus { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? Payload { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
