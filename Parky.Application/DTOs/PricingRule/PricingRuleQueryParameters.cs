using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parky.Application.Common;

namespace Parky.Application.DTOs.PricingRule;

public class PricingRuleQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ParkingId { get; set; }

    public string? PricingType { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    public bool? IsActive { get; set; }

    public int? MinPriority { get; set; }

    public int? MaxPriority { get; set; }

    public decimal? MinPricePerHour { get; set; }

    public decimal? MaxPricePerHour { get; set; }

    public decimal? MinFlatPrice { get; set; }

    public decimal? MaxFlatPrice { get; set; }

    public decimal? MinMaximumDailyPrice { get; set; }

    public decimal? MaxMaximumDailyPrice { get; set; }

    public DateTimeOffset? EffectiveAt { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? UpdatedFrom { get; set; }

    public DateTimeOffset? UpdatedTo { get; set; }
}

