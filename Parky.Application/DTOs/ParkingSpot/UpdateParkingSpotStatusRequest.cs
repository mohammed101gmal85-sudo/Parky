using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingSpot;

public sealed class UpdateParkingSpotStatusRequest
{
    public bool? IsAvailable { get; init; }

    public bool? IsActive { get; init; }
}

