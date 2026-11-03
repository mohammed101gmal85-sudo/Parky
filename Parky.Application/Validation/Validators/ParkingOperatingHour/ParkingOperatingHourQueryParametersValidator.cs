using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingOperatingHour;

namespace Parky.Application.Validation.ParkingOperatingHour;

public sealed class ParkingOperatingHourQueryParametersValidator
    : AbstractValidator<ParkingOperatingHourQueryParameters>
{
    public ParkingOperatingHourQueryParametersValidator()
    {
        RuleFor(x => x.ParkingId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking ID is invalid.");

        RuleFor(x => x.DayOfWeek)
            .Must(day =>
                !day.HasValue ||
                Enum.IsDefined(typeof(DayOfWeek), day.Value))
            .WithMessage("DayOfWeek must be a valid day of the week.");

        RuleFor(x => x.OpenTimeTo)
            .Must((parameters, openTimeTo) =>
                !parameters.OpenTimeFrom.HasValue ||
                !openTimeTo.HasValue ||
                parameters.OpenTimeFrom.Value <= openTimeTo.Value)
            .WithMessage(
                "OpenTimeFrom cannot be later than OpenTimeTo.");

        RuleFor(x => x.CloseTimeTo)
            .Must((parameters, closeTimeTo) =>
                !parameters.CloseTimeFrom.HasValue ||
                !closeTimeTo.HasValue ||
                parameters.CloseTimeFrom.Value <= closeTimeTo.Value)
            .WithMessage(
                "CloseTimeFrom cannot be later than CloseTimeTo.");

        RuleFor(x => x.CreatedTo)
            .Must((parameters, createdTo) =>
                !parameters.CreatedFrom.HasValue ||
                !createdTo.HasValue ||
                parameters.CreatedFrom.Value <= createdTo.Value)
            .WithMessage(
                "CreatedFrom cannot be later than CreatedTo.");

        RuleFor(x => x.UpdatedTo)
            .Must((parameters, updatedTo) =>
                !parameters.UpdatedFrom.HasValue ||
                !updatedTo.HasValue ||
                parameters.UpdatedFrom.Value <= updatedTo.Value)
            .WithMessage(
                "UpdatedFrom cannot be later than UpdatedTo.");

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");
    }
}