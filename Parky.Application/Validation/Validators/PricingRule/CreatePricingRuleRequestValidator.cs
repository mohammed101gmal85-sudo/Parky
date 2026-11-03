using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.PricingRule;

namespace Parky.Application.Validation.Validators.PricingRule;

public sealed class CreatePricingRuleRequestValidator
    : AbstractValidator<CreatePricingRuleRequest>
{
    private static readonly HashSet<string> AllowedPricingTypes =
        new(StringComparer.Ordinal)
        {
            "Hourly",
            "Flat",
            "Daily",
            "TimeBased"
        };

    public CreatePricingRuleRequestValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");

        ApplyCommonRules();
    }

    private void ApplyCommonRules()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Rule name is required.")
            .Must(value => value.Trim().Length <= 100)
            .WithMessage("Rule name cannot exceed 100 characters.");

        RuleFor(x => x.PricingType)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Pricing type is required.")
            .Must(value => AllowedPricingTypes.Contains(value.Trim()))
            .WithMessage("Unsupported pricing type.");

        RuleFor(x => x.PricePerHour)
            .InclusiveBetween(0m, 1_000_000m)
            .WithMessage("Price per hour is outside the allowed range.");

        RuleFor(x => x.FlatPrice)
            .Must(value =>
                !value.HasValue ||
                (value.Value >= 0 && value.Value <= 100_000_000m))
            .WithMessage("Flat price is outside the allowed range.");

        RuleFor(x => x.MaximumDailyPrice)
            .Must(value =>
                !value.HasValue ||
                (value.Value >= 0 && value.Value <= 100_000_000m))
            .WithMessage("Maximum daily price is outside the allowed range.");

        RuleFor(x => x.EffectiveTo)
            .Must((request, effectiveTo) =>
                !request.EffectiveFrom.HasValue ||
                !effectiveTo.HasValue ||
                effectiveTo.Value > request.EffectiveFrom.Value)
            .WithMessage("EffectiveTo must be later than EffectiveFrom.");

        RuleFor(x => x.EndTime)
            .Must((request, endTime) =>
                request.StartTime.HasValue == endTime.HasValue)
            .WithMessage("StartTime and EndTime must both be provided.");

        RuleFor(x => x.EndTime)
            .Must((request, endTime) =>
                !request.StartTime.HasValue ||
                !endTime.HasValue ||
                request.StartTime.Value != endTime.Value)
            .WithMessage("StartTime and EndTime cannot be equal.");

        RuleFor(x => x.PricePerHour)
            .GreaterThan(0m)
            .When(x => x.PricingType == "Hourly")
            .WithMessage("Hourly pricing requires a positive PricePerHour.");

        RuleFor(x => x.FlatPrice)
            .Must(value => value.HasValue && value.Value > 0)
            .When(x => x.PricingType == "Flat")
            .WithMessage("Flat pricing requires a positive FlatPrice.");

        RuleFor(x => x.MaximumDailyPrice)
            .Must((request, maximumDailyPrice) =>
                (maximumDailyPrice.HasValue && maximumDailyPrice.Value > 0) ||
                (request.FlatPrice.HasValue && request.FlatPrice.Value > 0))
            .When(x => x.PricingType == "Daily")
            .WithMessage(
                "Daily pricing requires a positive MaximumDailyPrice or FlatPrice.");

        RuleFor(x => x.StartTime)
            .Must((request, startTime) =>
                startTime.HasValue && request.EndTime.HasValue)
            .When(x => x.PricingType == "TimeBased")
            .WithMessage(
                "Time-based pricing requires StartTime and EndTime.");

        RuleFor(x => x.PricePerHour)
            .GreaterThan(0m)
            .When(x => x.PricingType == "TimeBased")
            .WithMessage(
                "Time-based pricing requires a positive PricePerHour.");
    }
}

