using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Parking;

public sealed class UpdateParkingRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Governorate { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Address { get; init; } = string.Empty;

    [Range(typeof(decimal), "-90", "90")]
    public decimal Latitude { get; init; }

    [Range(typeof(decimal), "-180", "180")]
    public decimal Longitude { get; init; }
}
