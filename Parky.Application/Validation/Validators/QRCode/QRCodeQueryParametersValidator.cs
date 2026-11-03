using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.QRCode;

namespace Parky.Application.Validation.Validators.QRCode;

public sealed class QRCodeQueryParametersValidator
    : AbstractValidator<QRCodeQueryParameters>
{
    public QRCodeQueryParametersValidator()
    {
        RuleFor(x => x.CreatedFrom)
            .Must((parameters, createdFrom) =>
                !createdFrom.HasValue ||
                !parameters.CreatedTo.HasValue ||
                createdFrom.Value <= parameters.CreatedTo.Value)
            .WithMessage("Created from date cannot be later than created to date.");

        RuleFor(x => x.ExpiresFrom)
            .Must((parameters, expiresFrom) =>
                !expiresFrom.HasValue ||
                !parameters.ExpiresTo.HasValue ||
                expiresFrom.Value <= parameters.ExpiresTo.Value)
            .WithMessage("Expires from date cannot be later than expires to date.");

        RuleFor(x => x.MinUsageCount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Minimum usage count cannot be negative.");

        RuleFor(x => x.MaxUsageCount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Maximum usage count cannot be negative.");

        RuleFor(x => x.MinUsageCount)
            .Must((parameters, minUsageCount) =>
                !minUsageCount.HasValue ||
                !parameters.MaxUsageCount.HasValue ||
                minUsageCount.Value <= parameters.MaxUsageCount.Value)
            .WithMessage("Minimum usage count cannot be greater than maximum usage count.");
    }
}