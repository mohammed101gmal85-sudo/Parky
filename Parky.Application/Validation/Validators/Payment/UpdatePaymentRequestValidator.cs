using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Payment;

namespace Parky.Application.Validation.Validators.Payment;

public sealed class UpdatePaymentRequestValidator
    : AbstractValidator<UpdatePaymentRequest>
{
    private static readonly HashSet<string> AllowedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Cash",
            "Card",
            "Wallet",
            "BankTransfer",
            "Online"
        };

    public UpdatePaymentRequestValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payment method is required.")
            .Must(value => value.Trim().Length <= 30)
            .WithMessage("Payment method cannot exceed 30 characters.")
            .Must(value => AllowedPaymentMethods.Contains(value.Trim()))
            .WithMessage("Unsupported payment method.");

        RuleFor(x => x.PaymentProvider)
            .MaximumLength(100)
            .WithMessage("Payment provider cannot exceed 100 characters.");
    }
}
