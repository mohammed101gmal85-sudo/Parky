using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.QRCode;

namespace Parky.Application.Validation.Validators.QRCode;

public sealed class ValidateQRCodeRequestValidator
    : AbstractValidator<ValidateQRCodeRequest>
{
    public ValidateQRCodeRequestValidator()
    {
        RuleFor(x => x.Token)
            .Cascade(CascadeMode.Stop)
            .Must(token => !string.IsNullOrWhiteSpace(token))
            .WithMessage("QR code token is required.")
            .Length(32, 512)
            .WithMessage("QR code token must be between 32 and 512 characters.");
    }
}