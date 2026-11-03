using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.QRCode;

public class ValidateQRCodeRequest
{
    [Required]
    [MinLength(32)]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;
}

