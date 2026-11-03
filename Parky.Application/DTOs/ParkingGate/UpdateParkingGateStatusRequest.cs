using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingGate;

public sealed class UpdateParkingGateStatusRequest
{
    public bool IsActive { get; init; }
}

