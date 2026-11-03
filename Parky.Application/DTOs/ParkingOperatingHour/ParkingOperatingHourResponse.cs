using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingOperatingHour;

public sealed class ParkingOperatingHourResponse
{
    public Guid Id { get; init; }

    public Guid ParkingId { get; init; }

    public DayOfWeek DayOfWeek { get; init; }

    public TimeOnly OpenTime { get; init; }

    public TimeOnly CloseTime { get; init; }

    public bool IsClosed { get; init; }

    public bool IsOpen24Hours { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
