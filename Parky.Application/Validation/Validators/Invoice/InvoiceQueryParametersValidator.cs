using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Invoice;

namespace Parky.Application.Validation.Validators.Invoice;

public sealed class InvoiceQueryParametersValidator
    : AbstractValidator<InvoiceQueryParameters>
{
    public InvoiceQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.ReservationId)
            .Must(value => !value.HasValue || value.Value != Guid.Empty)
            .WithMessage("Reservation ID cannot be empty.");

        RuleFor(x => x.PaymentId)
            .Must(value => !value.HasValue || value.Value != Guid.Empty)
            .WithMessage("Payment ID cannot be empty.");

        RuleFor(x => x.MinTotalAmount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Minimum total amount cannot be negative.");

        RuleFor(x => x.MaxTotalAmount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Maximum total amount cannot be negative.");

        RuleFor(x => x.MinTotalAmount)
            .Must((parameters, minAmount) =>
                !minAmount.HasValue ||
                !parameters.MaxTotalAmount.HasValue ||
                minAmount.Value <= parameters.MaxTotalAmount.Value)
            .WithMessage("Minimum total amount cannot be greater than maximum total amount.");

        RuleFor(x => x.IssuedFrom)
            .Must((parameters, issuedFrom) =>
                !issuedFrom.HasValue ||
                !parameters.IssuedTo.HasValue ||
                issuedFrom.Value <= parameters.IssuedTo.Value)
            .WithMessage("Issued from date cannot be greater than issued to date.");

        RuleFor(x => x.DueFrom)
            .Must((parameters, dueFrom) =>
                !dueFrom.HasValue ||
                !parameters.DueTo.HasValue ||
                dueFrom.Value <= parameters.DueTo.Value)
            .WithMessage("Due from date cannot be greater than due to date.");
    }
}

