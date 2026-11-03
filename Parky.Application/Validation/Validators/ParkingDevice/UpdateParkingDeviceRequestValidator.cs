using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingDevice;

namespace Parky.Application.Validation.ParkingDevice;

public sealed class UpdateParkingDeviceRequestValidator
    : AbstractValidator<UpdateParkingDeviceRequest>
{
    public UpdateParkingDeviceRequestValidator()
    {
        RuleFor(x => x.ParkingGateId)
            .NotEmpty()
            .WithMessage("Parking gate ID is required.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Device name is required.")
            .Length(2, 150);

        RuleFor(x => x.DeviceCode)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Device code is required.")
            .Length(2, 100);

        RuleFor(x => x.DeviceType)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Device type is required.")
            .Length(2, 50);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100);

        RuleFor(x => x.IpAddress)
            .MaximumLength(45);
    }
}