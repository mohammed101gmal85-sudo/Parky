using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.PricingRule;

public class PricingRuleResponse
{
    public Guid Id { get; set; }

    public Guid ParkingId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string PricingType { get; set; } = string.Empty;

    public decimal PricePerHour { get; set; }

    public decimal? FlatPrice { get; set; }

    public decimal? MaximumDailyPrice { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
