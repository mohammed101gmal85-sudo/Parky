using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingOperatingHour;

namespace Parky.Application.Validation.ParkingOperatingHour;

public sealed class UpdateParkingOperatingHourStatusRequestValidator
    : AbstractValidator<UpdateParkingOperatingHourStatusRequest>
{
    public UpdateParkingOperatingHourStatusRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !(x.IsClosed && x.IsOpen24Hours))
            .WithMessage(
                "A parking day cannot be closed and open 24 hours at the same time.");
    }
}