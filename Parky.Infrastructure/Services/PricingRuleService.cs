using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.PricingRule;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Microsoft.Extensions.Options;
using Parky.Infrastructure.Caching;
using Parky.Infrastructure.Settings;

namespace Parky.Infrastructure.Services;

public class PricingRuleService : IPricingRuleService
{
    private static readonly HashSet<string> AllowedPricingTypes =
        new(StringComparer.Ordinal)
        {
            "Hourly",
            "Flat",
            "Daily",
            "TimeBased"
        };

    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IParkingRepository _parkingRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public PricingRuleService(
        IPricingRuleRepository pricingRuleRepository,
        IParkingRepository parkingRepository,
        IMapper mapper,
        ICacheService cacheService,
        IOptions<CacheOptions> cacheOptions)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _parkingRepository = parkingRepository;
        _mapper = mapper;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }

    // ----------------------------------------------------
    // Create Pricing Rule
    // ----------------------------------------------------

    public async Task<PricingRuleResponse> CreateAsync(
        CreatePricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRule(
            request.PricingType,
            request.PricePerHour,
            request.FlatPrice,
            request.MaximumDailyPrice,
            request.StartTime,
            request.EndTime,
            request.EffectiveFrom,
            request.EffectiveTo);

        if (request.ParkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Rule name is required and cannot exceed 100 characters.");
        }

        var parking = await _parkingRepository.GetByIdAsync(
            request.ParkingId,
            cancellationToken);

        if (parking is null)
        {
            throw new KeyNotFoundException(
                "Parking was not found.");
        }

        var pricingRule = new PricingRule
        {
            ParkingId = request.ParkingId,
            Name = request.Name.Trim(),
            PricingType = request.PricingType.Trim(),
            PricePerHour = request.PricePerHour,
            FlatPrice = request.FlatPrice,
            MaximumDailyPrice = request.MaximumDailyPrice,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            DayOfWeek = request.DayOfWeek,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Priority = request.Priority,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _pricingRuleRepository.AddAsync(
            pricingRule,
            cancellationToken);

        return MapToResponse(pricingRule);
    }

    // ----------------------------------------------------
    // Get Pricing Rule By Id
    // ----------------------------------------------------

    public async Task<PricingRuleResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        return pricingRule is null
            ? null
            : MapToResponse(pricingRule);
    }

    // ----------------------------------------------------
    // Get All Pricing Rules
    // ----------------------------------------------------

    public async Task<IReadOnlyList<PricingRuleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rules = await _pricingRuleRepository.GetAllAsync(
            cancellationToken);

        return _mapper.Map<List<PricingRuleResponse>>(rules);
    }

    // ----------------------------------------------------
    // Get All Pricing Rules With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<PricingRuleResponse>> GetAllAsync(
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _pricingRuleRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Pricing Rules By Parking Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<PricingRuleResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        var parking = await _parkingRepository.GetByIdAsync(
            parkingId,
            cancellationToken);

        if (parking is null)
        {
            throw new KeyNotFoundException(
                "Parking was not found.");
        }

        var rules = await _pricingRuleRepository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        return _mapper.Map<List<PricingRuleResponse>>(rules);
    }

    // ----------------------------------------------------
    // Get Pricing Rules By Parking Id With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<PricingRuleResponse>> GetByParkingIdAsync(
        Guid parkingId,
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.",
                nameof(parkingId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var parking = await _parkingRepository.GetByIdAsync(
            parkingId,
            cancellationToken);

        if (parking is null)
        {
            throw new KeyNotFoundException(
                "Parking was not found.");
        }

        var result = await _pricingRuleRepository.GetPagedByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Update Pricing Rule
    // ----------------------------------------------------

    public async Task<PricingRuleResponse?> UpdateAsync(
        Guid id,
        UpdatePricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRule(
            request.PricingType,
            request.PricePerHour,
            request.FlatPrice,
            request.MaximumDailyPrice,
            request.StartTime,
            request.EndTime,
            request.EffectiveFrom,
            request.EffectiveTo);

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Rule name is required and cannot exceed 100 characters.");
        }

        var pricingRule = await _pricingRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (pricingRule is null)
        {
            return null;
        }

        pricingRule.Name = request.Name.Trim();
        pricingRule.PricingType = request.PricingType.Trim();
        pricingRule.PricePerHour = request.PricePerHour;
        pricingRule.FlatPrice = request.FlatPrice;
        pricingRule.MaximumDailyPrice = request.MaximumDailyPrice;
        pricingRule.StartTime = request.StartTime;
        pricingRule.EndTime = request.EndTime;
        pricingRule.DayOfWeek = request.DayOfWeek;
        pricingRule.EffectiveFrom = request.EffectiveFrom;
        pricingRule.EffectiveTo = request.EffectiveTo;
        pricingRule.Priority = request.Priority;
        pricingRule.IsActive = request.IsActive;
        pricingRule.UpdatedAt = DateTimeOffset.UtcNow;

        await _pricingRuleRepository.UpdateAsync(
            pricingRule,
            cancellationToken);

        return MapToResponse(pricingRule);
    }

    // ----------------------------------------------------
    // Delete Pricing Rule
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (pricingRule is null)
        {
            return false;
        }

        await _pricingRuleRepository.DeleteAsync(
            pricingRule,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Set Pricing Rule Active Status
    // ----------------------------------------------------

    public async Task<bool> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (pricingRule is null)
        {
            return false;
        }

        pricingRule.IsActive = isActive;
        pricingRule.UpdatedAt = DateTimeOffset.UtcNow;

        await _pricingRuleRepository.UpdateAsync(
            pricingRule,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        PricingRuleQueryParameters parameters)
    {
        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.ParkingId.HasValue &&
            parameters.ParkingId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.PricingType) &&
            !AllowedPricingTypes.Contains(parameters.PricingType.Trim()))
        {
            throw new ArgumentException(
                "Unsupported pricing type filter.");
        }

        if (parameters.DayOfWeek.HasValue &&
            !Enum.IsDefined(
                typeof(DayOfWeek),
                parameters.DayOfWeek.Value))
        {
            throw new ArgumentException(
                "DayOfWeek must be a valid day of the week.");
        }

        if (parameters.MinPriority.HasValue &&
            parameters.MaxPriority.HasValue &&
            parameters.MinPriority.Value > parameters.MaxPriority.Value)
        {
            throw new ArgumentException(
                "MinPriority cannot be greater than MaxPriority.");
        }

        if (parameters.MinPricePerHour.HasValue &&
            parameters.MinPricePerHour.Value < 0)
        {
            throw new ArgumentException(
                "MinPricePerHour cannot be negative.");
        }

        if (parameters.MaxPricePerHour.HasValue &&
            parameters.MaxPricePerHour.Value < 0)
        {
            throw new ArgumentException(
                "MaxPricePerHour cannot be negative.");
        }

        if (parameters.MinPricePerHour.HasValue &&
            parameters.MaxPricePerHour.HasValue &&
            parameters.MinPricePerHour.Value >
            parameters.MaxPricePerHour.Value)
        {
            throw new ArgumentException(
                "MinPricePerHour cannot be greater than MaxPricePerHour.");
        }

        ValidateOptionalPriceRange(
            parameters.MinFlatPrice,
            parameters.MaxFlatPrice,
            "FlatPrice");

        ValidateOptionalPriceRange(
            parameters.MinMaximumDailyPrice,
            parameters.MaxMaximumDailyPrice,
            "MaximumDailyPrice");

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }

        if (parameters.UpdatedFrom.HasValue &&
            parameters.UpdatedTo.HasValue &&
            parameters.UpdatedFrom.Value > parameters.UpdatedTo.Value)
        {
            throw new ArgumentException(
                "UpdatedFrom cannot be later than UpdatedTo.");
        }
    }

    // ----------------------------------------------------
    // Validate Optional Price Range
    // ----------------------------------------------------

    private static void ValidateOptionalPriceRange(
        decimal? minimum,
        decimal? maximum,
        string propertyName)
    {
        if (minimum.HasValue && minimum.Value < 0)
        {
            throw new ArgumentException(
                $"{propertyName} minimum cannot be negative.");
        }

        if (maximum.HasValue && maximum.Value < 0)
        {
            throw new ArgumentException(
                $"{propertyName} maximum cannot be negative.");
        }

        if (minimum.HasValue &&
            maximum.HasValue &&
            minimum.Value > maximum.Value)
        {
            throw new ArgumentException(
                $"{propertyName} minimum cannot be greater than maximum.");
        }
    }

    // ----------------------------------------------------
    // Validate Pricing Rule
    // ----------------------------------------------------

    private static void ValidateRule(
        string pricingType,
        decimal pricePerHour,
        decimal? flatPrice,
        decimal? maximumDailyPrice,
        TimeOnly? startTime,
        TimeOnly? endTime,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo)
    {
        if (string.IsNullOrWhiteSpace(pricingType) ||
            !AllowedPricingTypes.Contains(pricingType.Trim()))
        {
            throw new ArgumentException(
                "Unsupported pricing type.");
        }

        if (pricePerHour < 0 || pricePerHour > 1_000_000m)
        {
            throw new ArgumentException(
                "Price per hour is outside the allowed range.");
        }

        if (flatPrice is < 0 or > 100_000_000m)
        {
            throw new ArgumentException(
                "Flat price is outside the allowed range.");
        }

        if (maximumDailyPrice is < 0 or > 100_000_000m)
        {
            throw new ArgumentException(
                "Maximum daily price is outside the allowed range.");
        }

        if (effectiveFrom.HasValue &&
            effectiveTo.HasValue &&
            effectiveTo.Value <= effectiveFrom.Value)
        {
            throw new ArgumentException(
                "EffectiveTo must be later than EffectiveFrom.");
        }

        if (startTime.HasValue != endTime.HasValue)
        {
            throw new ArgumentException(
                "StartTime and EndTime must both be provided.");
        }

        if (startTime.HasValue &&
            startTime.Value == endTime!.Value)
        {
            throw new ArgumentException(
                "StartTime and EndTime cannot be equal.");
        }

        switch (pricingType.Trim())
        {
            case "Hourly":
                if (pricePerHour <= 0)
                {
                    throw new ArgumentException(
                        "Hourly pricing requires a positive PricePerHour.");
                }
                break;

            case "Flat":
                if (!flatPrice.HasValue || flatPrice.Value <= 0)
                {
                    throw new ArgumentException(
                        "Flat pricing requires a positive FlatPrice.");
                }
                break;

            case "Daily":
                if ((!maximumDailyPrice.HasValue ||
                     maximumDailyPrice.Value <= 0) &&
                    (!flatPrice.HasValue ||
                     flatPrice.Value <= 0))
                {
                    throw new ArgumentException(
                        "Daily pricing requires a positive MaximumDailyPrice or FlatPrice.");
                }
                break;

            case "TimeBased":
                if (!startTime.HasValue || !endTime.HasValue)
                {
                    throw new ArgumentException(
                        "Time-based pricing requires StartTime and EndTime.");
                }

                if (pricePerHour <= 0)
                {
                    throw new ArgumentException(
                        "Time-based pricing requires a positive PricePerHour.");
                }
                break;
        }
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private PricingRuleResponse MapToResponse(
        PricingRule pricingRule)
    {
        return _mapper.Map<PricingRuleResponse>(pricingRule);
    }

    private PagedResult<PricingRuleResponse> MapToPagedResponse(
        PagedResult<PricingRule> result)
    {
        return new PagedResult<PricingRuleResponse>
        {
            Items = _mapper.Map<List<PricingRuleResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
    private async Task InvalidatePricingRuleCacheAsync(
    Guid parkingId,
    CancellationToken cancellationToken)
    {
        await _cacheService.RemoveByPrefixAsync(
            CacheKeys.PricingRulesPrefix,
            cancellationToken);
    }
}