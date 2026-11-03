using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.QRCode;

namespace Parky.Application.Validation.Validators.QRCode;

public sealed class CreateQRCodeRequestValidator
    : AbstractValidator<CreateQRCodeRequest>
{
    public CreateQRCodeRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Reservation ID is required.");

        RuleFor(x => x.ValidityMinutes)
            .InclusiveBetween(1, 720)
            .WithMessage("Validity minutes must be between 1 and 720.");
    }
}