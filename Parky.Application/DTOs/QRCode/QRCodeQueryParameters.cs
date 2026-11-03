using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.QRCode;

public sealed class QRCodeQueryParameters : PaginationRequest
{
    public bool? IsRevoked { get; set; }

    public bool? IsUsed { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? ExpiresFrom { get; set; }

    public DateTimeOffset? ExpiresTo { get; set; }

    public int? MinUsageCount { get; set; }

    public int? MaxUsageCount { get; set; }
}
