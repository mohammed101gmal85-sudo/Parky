using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Vehicle;

public class SetDefaultVehicleRequest
{
    [Required]
    public Guid VehicleId { get; set; }
}

