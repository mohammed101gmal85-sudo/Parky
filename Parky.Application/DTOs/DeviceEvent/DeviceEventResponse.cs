using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.DTOs.DeviceEvent;

public class DeviceEventResponse
{
    public Guid Id { get; set; }

    public Guid ParkingDeviceId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventStatus { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Payload { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }
}