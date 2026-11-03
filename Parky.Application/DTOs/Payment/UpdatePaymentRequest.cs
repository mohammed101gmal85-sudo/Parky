using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Payment;

public class UpdatePaymentRequest
{
    [Required]
    [StringLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PaymentProvider { get; set; }
}

