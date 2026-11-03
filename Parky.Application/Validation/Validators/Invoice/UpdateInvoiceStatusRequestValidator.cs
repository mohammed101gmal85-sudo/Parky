using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Invoice;

namespace Parky.Application.Validation.Validators.Invoice;

public sealed class UpdateInvoiceStatusRequestValidator
    : AbstractValidator<UpdateInvoiceStatusRequest>
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Draft",
            "Issued",
            "Paid",
            "Overdue",
            "Cancelled"
        };

    public UpdateInvoiceStatusRequestValidator()
    {
        RuleFor(x => x.InvoiceStatus)
            .Cascade(CascadeMode.Stop)
            .Must(status => !string.IsNullOrWhiteSpace(status))
            .WithMessage("Invoice status is required.")
            .MaximumLength(50)
            .WithMessage("Invoice status cannot exceed 50 characters.")
            .Must(status => AllowedStatuses.Contains(status.Trim()))
            .WithMessage("Invalid invoice status.");
    }
}
