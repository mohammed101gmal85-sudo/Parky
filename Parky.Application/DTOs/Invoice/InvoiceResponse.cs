using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.DTOs.Invoice;

public class InvoiceResponse
{
    public Guid Id { get; set; }

    public Guid ReservationId { get; set; }

    public Guid? PaymentId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "EGP";

    public string InvoiceStatus { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
