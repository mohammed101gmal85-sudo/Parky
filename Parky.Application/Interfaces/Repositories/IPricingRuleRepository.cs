
using Parky.Application.Common;
using Parky.Application.DTOs.PricingRule;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IPricingRuleRepository
{
    Task<PricingRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingRule>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<PricingRule>> GetPagedAsync(
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingRule>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PricingRule>> GetPagedByParkingIdAsync(
        Guid parkingId,
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PricingRule pricingRule,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PricingRule pricingRule,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PricingRule pricingRule,
        CancellationToken cancellationToken = default);
}
