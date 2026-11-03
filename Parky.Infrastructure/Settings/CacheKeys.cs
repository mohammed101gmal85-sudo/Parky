namespace Parky.Infrastructure.Caching;

public static class CacheKeys
{
    public const string ParkingsPrefix = "Parkings:";
    public const string PricingRulesPrefix = "PricingRules:";
    public const string ParkingAvailabilityPrefixValue =
        "ParkingAvailability:";

    public static string ParkingById(Guid parkingId)
    {
        return $"{ParkingsPrefix}{parkingId}";
    }

    public static string ParkingList(string queryKey = "All")
    {
        return $"{ParkingsPrefix}List:{queryKey}";
    }

    public static string ParkingListPrefix()
    {
        return $"{ParkingsPrefix}List:";
    }

    public static string ParkingRelatedPrefix(Guid parkingId)
    {
        return $"{ParkingsPrefix}{parkingId}:";
    }

    public static string ParkingFloors(Guid parkingId)
    {
        return $"{ParkingRelatedPrefix(parkingId)}Floors";
    }

    public static string ParkingSpots(Guid parkingId)
    {
        return $"{ParkingRelatedPrefix(parkingId)}Spots";
    }

    public static string ParkingAvailabilityPrefix()
    {
        return ParkingAvailabilityPrefixValue;
    }

    public static string ParkingAvailability(
        Guid parkingFloorId,
        string queryKey)
    {
        return $"{ParkingAvailabilityPrefixValue}Floor:{parkingFloorId}:{queryKey}";
    }

    public static string ParkingFloorSpotsPrefix(Guid parkingFloorId)
    {
        return $"ParkingFloor:{parkingFloorId}:Spots:";
    }

    public static string ParkingSpotById(Guid parkingSpotId)
    {
        return $"ParkingSpots:{parkingSpotId}";
    }

    public static string ParkingAvailabilityByFloorPrefix(
        Guid parkingFloorId)
    {
        return $"{ParkingAvailabilityPrefixValue}Floor:{parkingFloorId}:";
    }

    public static string PricingRulesByParking(Guid parkingId)
    {
        return $"{PricingRulesPrefix}Parking:{parkingId}";
    }

    public static string AllPricingRules()
    {
        return $"{PricingRulesPrefix}All";
    }

    public static string PricingRuleById(Guid pricingRuleId)
    {
        return $"{PricingRulesPrefix}Id:{pricingRuleId}";
    }

    public static string PricingRulesByParkingPrefix(Guid parkingId)
    {
        return $"{PricingRulesPrefix}Parking:{parkingId}";
    }
}