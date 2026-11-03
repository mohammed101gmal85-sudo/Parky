using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.ParkingDevice;

public sealed class ParkingDeviceQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ParkingGateId { get; set; }

    public string? DeviceType { get; set; }

    public bool? IsOnline { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? LastSeenFrom { get; set; }

    public DateTimeOffset? LastSeenTo { get; set; }

    public DateTimeOffset? UpdatedFrom { get; set; }

    public DateTimeOffset? UpdatedTo { get; set; }
}
