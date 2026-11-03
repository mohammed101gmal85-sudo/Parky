using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Payment;

namespace Parky.Application.Validation.Validators.Payment;

public sealed class UpdatePaymentStatusRequestValidator
    : AbstractValidator<UpdatePaymentStatusRequest>
{
    private static readonly HashSet<string> AllowedPaymentStatuses =
        new(StringComparer.Ordinal)
        {
            "Pending",
            "Processing",
            "Succeeded",
            "Failed",
            "Cancelled",
            "Refunded",
            "PartiallyRefunded"
        };

    public UpdatePaymentStatusRequestValidator()
    {
        RuleFor(x => x.PaymentStatus)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payment status is required.")
            .Must(value => AllowedPaymentStatuses.Contains(value.Trim()))
            .WithMessage("Invalid payment status.");

        RuleFor(x => x.ProviderTransactionId)
            .MaximumLength(200)
            .WithMessage("Provider transaction ID cannot exceed 200 characters.");

        RuleFor(x => x.FailureReason)
            .MaximumLength(500)
            .WithMessage("Failure reason cannot exceed 500 characters.");
    }
}
