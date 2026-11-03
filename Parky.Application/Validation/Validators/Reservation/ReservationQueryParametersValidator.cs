using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Reservation;

namespace Parky.Application.Validation.Validators.Reservation;

public sealed class ReservationQueryParametersValidator
    : AbstractValidator<ReservationQueryParameters>
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending",
            "Confirmed",
            "Active",
            "Completed",
            "Cancelled"
        };

    public ReservationQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term cannot exceed 50 characters.");

        RuleFor(x => x.Status)
            .Must(status =>
                string.IsNullOrWhiteSpace(status) ||
                AllowedStatuses.Contains(status.Trim()))
            .WithMessage("Invalid reservation status.");

        RuleFor(x => x.ParkingId)
            .Must(parkingId =>
                !parkingId.HasValue ||
                parkingId.Value != Guid.Empty)
            .WithMessage("Parking ID cannot be empty.");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate!.Value)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date cannot be greater than To date.");
    }
}
