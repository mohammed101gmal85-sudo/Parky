using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingDevice;

namespace Parky.Application.Validation.ParkingDevice;

public sealed class ParkingDeviceQueryParametersValidator
    : AbstractValidator<ParkingDeviceQueryParameters>
{
    public ParkingDeviceQueryParametersValidator()
    {
        RuleFor(x => x.ParkingGateId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking gate ID cannot be empty.");

        RuleFor(x => x.DeviceType)
            .MaximumLength(50);

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.CreatedTo)
            .Must((parameters, createdTo) =>
                !parameters.CreatedFrom.HasValue ||
                !createdTo.HasValue ||
                createdTo.Value >= parameters.CreatedFrom.Value)
            .WithMessage("Created to date must be greater than or equal to created from date.");

        RuleFor(x => x.LastSeenTo)
            .Must((parameters, lastSeenTo) =>
                !parameters.LastSeenFrom.HasValue ||
                !lastSeenTo.HasValue ||
                lastSeenTo.Value >= parameters.LastSeenFrom.Value)
            .WithMessage("Last seen to date must be greater than or equal to last seen from date.");

        RuleFor(x => x.UpdatedTo)
            .Must((parameters, updatedTo) =>
                !parameters.UpdatedFrom.HasValue ||
                !updatedTo.HasValue ||
                updatedTo.Value >= parameters.UpdatedFrom.Value)
            .WithMessage("Updated to date must be greater than or equal to updated from date.");
    }
}