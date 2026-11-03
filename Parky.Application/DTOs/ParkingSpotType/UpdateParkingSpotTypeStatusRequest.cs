using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingSpotType;

public sealed class UpdateParkingSpotTypeStatusRequest
{
    public bool IsActive { get; init; }
}
