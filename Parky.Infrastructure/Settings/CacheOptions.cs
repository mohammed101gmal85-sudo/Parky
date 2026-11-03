using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Parky.Infrastructure.Settings;

public sealed class CacheOptions
{
    public TimeSpan DefaultExpiration { get; set; } =
        TimeSpan.FromMinutes(5);

    public TimeSpan ParkingExpiration { get; set; } =
        TimeSpan.FromMinutes(5);

    public TimeSpan ParkingFloorsExpiration { get; set; } =
        TimeSpan.FromMinutes(15);

    public TimeSpan ParkingSpotTypesExpiration { get; set; } =
        TimeSpan.FromMinutes(30);

    public TimeSpan ParkingAvailabilityExpiration { get; set; } =
        TimeSpan.FromSeconds(15);

    public TimeSpan PricingRulesExpiration { get; set; } =
        TimeSpan.FromMinutes(5);
}