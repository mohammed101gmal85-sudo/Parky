using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.Invoice;

public sealed class InvoiceQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ReservationId { get; set; }

    public Guid? PaymentId { get; set; }

    public string? InvoiceStatus { get; set; }

    public string? Currency { get; set; }

    public decimal? MinTotalAmount { get; set; }

    public decimal? MaxTotalAmount { get; set; }

    public DateTimeOffset? IssuedFrom { get; set; }

    public DateTimeOffset? IssuedTo { get; set; }

    public DateTimeOffset? DueFrom { get; set; }

    public DateTimeOffset? DueTo { get; set; }
}
