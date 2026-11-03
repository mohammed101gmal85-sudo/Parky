using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Notification;

namespace Parky.Application.Validation.Validators.Notification;

public sealed class NotificationQueryParametersValidator
    : AbstractValidator<NotificationQueryParameters>
{
    public NotificationQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.NotificationType)
            .MaximumLength(50)
            .When(x => x.NotificationType is not null)
            .WithMessage("Notification type cannot exceed 50 characters.");

        RuleFor(x => x.CreatedFrom)
            .Must((parameters, createdFrom) =>
                !createdFrom.HasValue ||
                !parameters.CreatedTo.HasValue ||
                createdFrom.Value <= parameters.CreatedTo.Value)
            .WithMessage("Created from date cannot be later than created to date.");

        RuleFor(x => x.SentFrom)
            .Must((parameters, sentFrom) =>
                !sentFrom.HasValue ||
                !parameters.SentTo.HasValue ||
                sentFrom.Value <= parameters.SentTo.Value)
            .WithMessage("Sent from date cannot be later than sent to date.");

        RuleFor(x => x.ExpiresFrom)
            .Must((parameters, expiresFrom) =>
                !expiresFrom.HasValue ||
                !parameters.ExpiresTo.HasValue ||
                expiresFrom.Value <= parameters.ExpiresTo.Value)
            .WithMessage("Expires from date cannot be later than expires to date.");
    }
}
