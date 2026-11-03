using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Invoice;

namespace Parky.Application.Validation.Validators.Invoice;

public sealed class UpdateInvoiceRequestValidator
    : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .Must(value => !value.HasValue || value.Value != Guid.Empty)
            .WithMessage("Payment ID is invalid.");

        RuleFor(x => x.Subtotal)
            .InclusiveBetween(0m, 999_999_999m)
            .WithMessage("Subtotal must be between 0 and 999,999,999.");

        RuleFor(x => x.DiscountAmount)
            .InclusiveBetween(0m, 999_999_999m)
            .WithMessage("Discount amount must be between 0 and 999,999,999.");

        RuleFor(x => x.TaxAmount)
            .InclusiveBetween(0m, 999_999_999m)
            .WithMessage("Tax amount must be between 0 and 999,999,999.");

        RuleFor(x => x.DiscountAmount)
            .LessThanOrEqualTo(x => x.Subtotal)
            .WithMessage("Discount cannot exceed subtotal.");

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Currency is required.")
            .Must(value => value.Trim().Length == 3)
            .WithMessage("Currency must be a three-character code, such as EGP.");

        RuleFor(x => x.InvoiceStatus)
            .MaximumLength(50)
            .WithMessage("Invoice status cannot exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Invoice notes cannot exceed 1000 characters.");
    }
}
