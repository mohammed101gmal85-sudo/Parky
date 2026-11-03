using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Payment;

namespace Parky.Application.Validation.Validators.Payment;

public sealed class CreatePaymentRequestValidator
    : AbstractValidator<CreatePaymentRequest>
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

    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Reservation ID is required.");

        RuleFor(x => x.PaymentNumber)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payment number is required.")
            .Must(value => value.Trim().Length <= 100)
            .WithMessage("Payment number cannot exceed 100 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000_000m)
            .WithMessage("Payment amount is too large.");

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Currency is required.")
            .Must(value => value.Trim().Length == 3)
            .WithMessage("Currency must contain exactly three characters.")
            .Must(value => value.Trim().All(c => c is >= 'A' and <= 'Z'))
            .WithMessage("Currency must be a three-letter uppercase currency code.");

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

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(200)
            .WithMessage("Idempotency key cannot exceed 200 characters.");
    }
}
