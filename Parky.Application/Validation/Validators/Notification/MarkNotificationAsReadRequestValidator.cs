using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Notification;

namespace Parky.Application.Validation.Validators.Notification;

public sealed class MarkNotificationAsReadRequestValidator
    : AbstractValidator<MarkNotificationAsReadRequest>
{
    public MarkNotificationAsReadRequestValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}