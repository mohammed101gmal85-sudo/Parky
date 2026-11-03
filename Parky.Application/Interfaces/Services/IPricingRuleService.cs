
using Parky.Application.Common;
using Parky.Application.DTOs.PricingRule;

namespace Parky.Application.Interfaces.Services;

public interface IPricingRuleService
{
    Task<PricingRuleResponse> CreateAsync(
        CreatePricingRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<PricingRuleResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingRuleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<PricingRuleResponse>> GetAllAsync(
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingRuleResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PricingRuleResponse>> GetByParkingIdAsync(
        Guid parkingId,
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PricingRuleResponse?> UpdateAsync(
        Guid id,
        UpdatePricingRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
