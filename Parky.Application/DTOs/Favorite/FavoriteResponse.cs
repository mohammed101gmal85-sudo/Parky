using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.DTOs.Favorite;

public class FavoriteResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ParkingId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}