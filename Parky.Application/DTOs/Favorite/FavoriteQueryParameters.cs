using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.Favorite;

public sealed class FavoriteQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ParkingId { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }
}
