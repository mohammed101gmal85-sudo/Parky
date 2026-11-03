using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.UserVehicle;

public class UserVehicleResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid VehicleId { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
