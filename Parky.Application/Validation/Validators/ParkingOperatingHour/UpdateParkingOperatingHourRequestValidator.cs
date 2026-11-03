using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingOperatingHour;

namespace Parky.Application.Validation.ParkingOperatingHour;

public sealed class UpdateParkingOperatingHourRequestValidator
    : AbstractValidator<UpdateParkingOperatingHourRequest>
{
    public UpdateParkingOperatingHourRequestValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum()
            .WithMessage("DayOfWeek must be a valid day of the week.");

        RuleFor(x => x)
            .Must(x => !(x.IsClosed && x.IsOpen24Hours))
            .WithMessage(
                "A parking day cannot be closed and open 24 hours at the same time.");

        RuleFor(x => x)
            .Must(x =>
                x.IsClosed ||
                x.IsOpen24Hours ||
                x.OpenTime < x.CloseTime)
            .WithMessage(
                "OpenTime must be earlier than CloseTime for normal operating hours.");
    }
}