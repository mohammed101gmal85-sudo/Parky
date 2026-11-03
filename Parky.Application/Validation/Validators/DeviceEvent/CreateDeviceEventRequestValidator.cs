using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.DeviceEvent;

namespace Parky.Application.Validation.Validators.DeviceEvent;

public sealed class CreateDeviceEventRequestValidator
    : AbstractValidator<CreateDeviceEventRequest>
{
    public CreateDeviceEventRequestValidator()
    {
        RuleFor(x => x.ParkingDeviceId)
            .NotEmpty()
            .WithMessage("Parking device ID is required.");

        RuleFor(x => x.EventType)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Event type is required.")
            .Must(value => value.Trim().Length <= 100)
            .WithMessage("Event type cannot exceed 100 characters.");

        RuleFor(x => x.EventStatus)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Event status is required.")
            .Must(value => value.Trim().Length <= 50)
            .WithMessage("Event status cannot exceed 50 characters.");

        RuleFor(x => x.Description)
            .Must(value => value is null || value.Length <= 1000)
            .WithMessage("Description cannot exceed 1000 characters.");
    }
}
