using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.PricingRule;

public class UpdatePricingRuleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("Hourly|Flat|Daily|TimeBased",
        ErrorMessage = "Pricing type must be Hourly, Flat, Daily, or TimeBased.")]
    public string PricingType { get; set; } = "Hourly";

    [Range(typeof(decimal), "0", "1000000")]
    public decimal PricePerHour { get; set; }

    [Range(typeof(decimal), "0", "100000000")]
    public decimal? FlatPrice { get; set; }

    [Range(typeof(decimal), "0", "100000000")]
    public decimal? MaximumDailyPrice { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    [Range(0, 100000)]
    public int Priority { get; set; }

    public bool IsActive { get; set; }
}

