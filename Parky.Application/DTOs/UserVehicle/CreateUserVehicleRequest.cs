using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.UserVehicle;

public class CreateUserVehicleRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    public bool IsDefault { get; set; }
}
