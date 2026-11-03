using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingSpotType;

namespace Parky.Application.Validation.ParkingSpotType;

public sealed class UpdateParkingSpotTypeRequestValidator
    : AbstractValidator<UpdateParkingSpotTypeRequest>
{
    public UpdateParkingSpotTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Name is required.")
            .Must(x => x.Trim().Length >= 2 && x.Trim().Length <= 100)
            .WithMessage("Name must contain between 2 and 100 characters.");

        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Code is required.")
            .Must(x => x.Trim().Length >= 2 && x.Trim().Length <= 30)
            .WithMessage("Code must contain between 2 and 30 characters.")
            .Must(IsValidCode)
            .WithMessage(
                "Code can contain only English letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.AdditionalHourlyRate)
            .Must(rate =>
                !rate.HasValue ||
                (rate.Value >= 0m && rate.Value <= 1000000m))
            .WithMessage(
                "Additional hourly rate must be between 0 and 1000000.");
    }

    private static bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim();

        return normalizedCode.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character == '_' ||
            character == '-');
    }
}