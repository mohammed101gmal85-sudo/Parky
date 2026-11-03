using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReservationId { get; set; }

    public Guid? PaymentId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? DueAt { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "EGP";

    public string InvoiceStatus { get; set; } = "Issued";

    public string? Notes { get; set; }

    public Reservation Reservation { get; set; } = null!;

    public Payment? Payment { get; set; }
}
