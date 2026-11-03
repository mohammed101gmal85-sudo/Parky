using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Favorite;

public class CreateFavoriteRequest
{
    [Required]
    public Guid ParkingId { get; set; }
}
