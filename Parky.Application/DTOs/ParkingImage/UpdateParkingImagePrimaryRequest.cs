using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingImage;

public sealed class UpdateParkingImagePrimaryRequest
{
    public bool IsPrimary { get; init; }
}
