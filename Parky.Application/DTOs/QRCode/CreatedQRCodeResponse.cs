using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.QRCode;

public class CreatedQRCodeResponse
{
    public QRCodeResponse QRCode { get; set; } = new();

    public string Token { get; set; } = string.Empty;
}
