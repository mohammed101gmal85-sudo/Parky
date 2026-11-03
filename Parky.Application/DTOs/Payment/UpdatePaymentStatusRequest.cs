using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Payment;

public class UpdatePaymentStatusRequest
{
    [Required]
    [RegularExpression(
        "^(Pending|Processing|Succeeded|Failed|Cancelled|Refunded|PartiallyRefunded)$",
        ErrorMessage = "Invalid payment status.")]
    public string PaymentStatus { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ProviderTransactionId { get; set; }

    [StringLength(500)]
    public string? FailureReason { get; set; }
}
