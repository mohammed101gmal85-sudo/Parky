using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingFloor;

public sealed class UpdateParkingFloorStatusRequest
{
    public bool IsActive { get; init; }
}
