using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Invoice;

public class UpdateInvoiceStatusRequest
{
    [Required]
    [StringLength(50)]
    public string InvoiceStatus { get; set; } = string.Empty;
}