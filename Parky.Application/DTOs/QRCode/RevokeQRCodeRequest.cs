using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.QRCode;

public class RevokeQRCodeRequest
{
    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
}

