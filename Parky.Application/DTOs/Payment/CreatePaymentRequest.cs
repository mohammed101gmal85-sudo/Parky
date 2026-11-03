using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Payment;

public class CreatePaymentRequest
{
    [Required]
    public Guid ReservationId { get; set; }

    [Required]
    [StringLength(100)]
    public string PaymentNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; set; } = "EGP";

    [Required]
    [StringLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PaymentProvider { get; set; }

    [StringLength(200)]
    public string? IdempotencyKey { get; set; }
}

