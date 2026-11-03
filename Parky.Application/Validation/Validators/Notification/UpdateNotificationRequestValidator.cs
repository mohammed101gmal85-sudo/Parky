using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Parky.Application.DTOs.Notification;

namespace Parky.Application.Validation.Validators.Notification;

public sealed class UpdateNotificationRequestValidator
    : AbstractValidator<UpdateNotificationRequest>
{
    public UpdateNotificationRequestValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Notification title is required.")
            .Must(value => value.Trim().Length <= 200)
            .WithMessage("Notification title cannot exceed 200 characters.");

        RuleFor(x => x.Message)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Notification message is required.")
            .Must(value => value.Trim().Length <= 2000)
            .WithMessage("Notification message cannot exceed 2000 characters.");

        RuleFor(x => x.NotificationType)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Notification type is required.")
            .Must(value => value.Trim().Length <= 50)
            .WithMessage("Notification type cannot exceed 50 characters.");

        RuleFor(x => x.ReferenceId)
            .MaximumLength(100)
            .WithMessage("Reference ID cannot exceed 100 characters.");

        RuleFor(x => x.ActionUrl)
            .MaximumLength(500)
            .WithMessage("Action URL cannot exceed 500 characters.");

        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt =>
                !expiresAt.HasValue ||
                expiresAt.Value > DateTimeOffset.UtcNow)
            .WithMessage("Expiration time must be in the future.");
    }
}
