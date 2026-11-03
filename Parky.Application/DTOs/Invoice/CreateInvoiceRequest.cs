using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Invoice;

public class CreateInvoiceRequest
{
    [Required]
    public Guid ReservationId { get; set; }

    public Guid? PaymentId { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Subtotal { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal DiscountAmount { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal TaxAmount { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EGP";

    [StringLength(2000)]
    public string? Notes { get; set; }
}