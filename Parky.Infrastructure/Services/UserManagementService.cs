using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.User;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Identity;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ParkyDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IMapper _mapper;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ParkyDbContext dbContext,
        TimeProvider timeProvider,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Get All Users
    // ----------------------------------------------------

    public async Task<PagedResult<UserManagementResponse>> GetAllAsync(
        UserQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        IQueryable<ApplicationUser> query = _userManager.Users
            .AsNoTracking();

        query = ApplyFilters(query, parameters);

        if (!string.IsNullOrWhiteSpace(parameters.RoleName))
        {
            var role = await _roleManager.FindByNameAsync(
                parameters.RoleName.Trim());

            if (role is null)
            {
                return CreateEmptyPagedResult(parameters);
            }

            var roleId = role.Id;

            query = query.Where(user =>
                _dbContext.Set<IdentityUserRole<Guid>>()
                    .Any(userRole =>
                        userRole.UserId == user.Id &&
                        userRole.RoleId == roleId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var items = await MapToResponsesAsync(
            users,
            cancellationToken);

        return new PagedResult<UserManagementResponse>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    // ----------------------------------------------------
    // Get User By Id
    // ----------------------------------------------------

    public async Task<UserManagementResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.");
        }

        var user = await _userManager.FindByIdAsync(
            id.ToString());

        if (user is null)
        {
            return null;
        }

        var responses = await MapToResponsesAsync(
            new[] { user },
            cancellationToken);

        return responses[0];
    }

    // ----------------------------------------------------
    // Update User Status
    // ----------------------------------------------------

    public async Task<UserManagementResponse?> UpdateStatusAsync(
        Guid id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.");
        }

        var user = await _userManager.FindByIdAsync(
            id.ToString());

        if (user is null)
        {
            return null;
        }

        if (user.IsActive == request.IsActive)
        {
            return await GetByIdAsync(id, cancellationToken);
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = _timeProvider.GetUtcNow();

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                updateResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"User status update failed: {errors}");
        }

        var responses = await MapToResponsesAsync(
            new[] { user },
            cancellationToken);

        return responses[0];
    }

    // ----------------------------------------------------
    // Apply Filters
    // ----------------------------------------------------

    private IQueryable<ApplicationUser> ApplyFilters(
        IQueryable<ApplicationUser> query,
        UserQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(user =>
                user.FullName.ToLower().Contains(searchTerm) ||
                (user.Email != null &&
                 user.Email.ToLower().Contains(searchTerm)) ||
                (user.UserName != null &&
                 user.UserName.ToLower().Contains(searchTerm)) ||
                (user.PhoneNumber != null &&
                 user.PhoneNumber.Contains(searchTerm)));
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(user =>
                user.IsActive == parameters.IsActive.Value);
        }

        if (parameters.EmailConfirmed.HasValue)
        {
            query = query.Where(user =>
                user.EmailConfirmed ==
                parameters.EmailConfirmed.Value);
        }

        if (parameters.PhoneNumberConfirmed.HasValue)
        {
            query = query.Where(user =>
                user.PhoneNumberConfirmed ==
                parameters.PhoneNumberConfirmed.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(user =>
                user.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(user =>
                user.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(user =>
                user.UpdatedAt.HasValue &&
                user.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(user =>
                user.UpdatedAt.HasValue &&
                user.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        UserQueryParameters parameters)
    {
        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.RoleName?.Length > 256)
        {
            throw new ArgumentException(
                "RoleName cannot exceed 256 characters.");
        }

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
    // Mapping
    // ----------------------------------------------------

    private async Task<List<UserManagementResponse>> MapToResponsesAsync(
        IReadOnlyCollection<ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0)
        {
            return new List<UserManagementResponse>();
        }

        var userIds = users
            .Select(user => user.Id)
            .ToList();

        var roleAssignments = await (
            from userRole in _dbContext.Set<IdentityUserRole<Guid>>()
                .AsNoTracking()
            join role in _dbContext.Set<IdentityRole<Guid>>()
                .AsNoTracking()
                on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new
            {
                userRole.UserId,
                RoleName = role.Name
            })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleAssignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(assignment => assignment.RoleName!)
                    .ToList());

        var responses = _mapper.Map<List<UserManagementResponse>>(users);

        foreach (var response in responses)
        {
            response.Roles = rolesByUser.TryGetValue(
                response.Id,
                out var roles)
                    ? roles
                    : Array.Empty<string>();
        }

        return responses;
    }

    // ----------------------------------------------------
    // Empty Paged Result
    // ----------------------------------------------------

    private static PagedResult<UserManagementResponse>
        CreateEmptyPagedResult(UserQueryParameters parameters)
    {
        return new PagedResult<UserManagementResponse>
        {
            Items = Array.Empty<UserManagementResponse>(),
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = 0
        };
    }
}