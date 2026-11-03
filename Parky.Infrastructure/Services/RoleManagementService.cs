using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Role;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Identity;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Services;

public sealed class RoleManagementService : IRoleManagementService
{
    private static readonly HashSet<string> SystemRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "User",
            "Owner",
            "Admin"
        };

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ParkyDbContext _dbContext;
    private readonly IMapper _mapper;

    public RoleManagementService(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        ParkyDbContext dbContext,
        IMapper mapper)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Get All Roles
    // ----------------------------------------------------

    public async Task<PagedResult<RoleManagementResponse>> GetAllAsync(
        RoleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateRoleQueryParameters(parameters);

        IQueryable<IdentityRole<Guid>> query = _roleManager.Roles
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(role =>
                role.Name != null &&
                role.Name.ToLower().Contains(searchTerm));
        }

        var userRoles = _dbContext.Set<IdentityUserRole<Guid>>()
            .AsNoTracking();

        if (parameters.MinUsersCount.HasValue)
        {
            var minimum = parameters.MinUsersCount.Value;

            query = query.Where(role =>
                userRoles.Count(userRole =>
                    userRole.RoleId == role.Id) >= minimum);
        }

        if (parameters.MaxUsersCount.HasValue)
        {
            var maximum = parameters.MaxUsersCount.Value;

            query = query.Where(role =>
                userRoles.Count(userRole =>
                    userRole.RoleId == role.Id) <= maximum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(role => role.Name)
            .ThenBy(role => role.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var roleIds = roles
            .Select(role => role.Id)
            .ToList();

        var roleCounts = await userRoles
            .Where(userRole => roleIds.Contains(userRole.RoleId))
            .GroupBy(userRole => userRole.RoleId)
            .Select(group => new
            {
                RoleId = group.Key,
                UsersCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var countsByRole = roleCounts.ToDictionary(
            item => item.RoleId,
            item => item.UsersCount);

        var items = roles
            .Select(role =>
            {
                var usersCount = countsByRole.TryGetValue(
                    role.Id,
                    out var count)
                        ? count
                        : 0;

                return MapToResponse(role, usersCount);
            })
            .ToList();

        return new PagedResult<RoleManagementResponse>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    // ----------------------------------------------------
    // Get Role By Id
    // ----------------------------------------------------

    public async Task<RoleManagementResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role ID is required.");
        }

        var role = await _roleManager.FindByIdAsync(
            id.ToString());

        if (role is null)
        {
            return null;
        }

        var usersCount = await _dbContext
            .Set<IdentityUserRole<Guid>>()
            .AsNoTracking()
            .CountAsync(
                userRole => userRole.RoleId == role.Id,
                cancellationToken);

        return MapToResponse(role, usersCount);
    }

    // ----------------------------------------------------
    // Create Role
    // ----------------------------------------------------

    public async Task<RoleManagementResponse> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var roleName = NormalizeRoleName(request.RoleName);

        var existingRole = await _roleManager.FindByNameAsync(
            roleName);

        if (existingRole is not null)
        {
            throw new InvalidOperationException(
                "A role with this name already exists.");
        }

        var role = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = roleName
        };

        var result = await _roleManager.CreateAsync(role);

        EnsureIdentitySucceeded(
            result,
            "Role creation failed");

        return MapToResponse(role, 0);
    }

    // ----------------------------------------------------
    // Update Role
    // ----------------------------------------------------

    public async Task<RoleManagementResponse?> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role ID is required.");
        }

        var role = await _roleManager.FindByIdAsync(
            id.ToString());

        if (role is null)
        {
            return null;
        }

        var newRoleName = NormalizeRoleName(request.RoleName);

        if (string.Equals(
                role.Name,
                newRoleName,
                StringComparison.OrdinalIgnoreCase))
        {
            return await GetByIdAsync(id, cancellationToken);
        }

        if (IsSystemRole(role.Name))
        {
            throw new InvalidOperationException(
                "System roles cannot be renamed.");
        }

        var existingRole = await _roleManager.FindByNameAsync(
            newRoleName);

        if (existingRole is not null &&
            existingRole.Id != role.Id)
        {
            throw new InvalidOperationException(
                "A role with this name already exists.");
        }

        role.Name = newRoleName;

        var result = await _roleManager.UpdateAsync(role);

        EnsureIdentitySucceeded(
            result,
            "Role update failed");

        return await GetByIdAsync(id, cancellationToken);
    }

    // ----------------------------------------------------
    // Delete Role
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role ID is required.");
        }

        var role = await _roleManager.FindByIdAsync(
            id.ToString());

        if (role is null)
        {
            return false;
        }

        if (IsSystemRole(role.Name))
        {
            throw new InvalidOperationException(
                "System roles cannot be deleted.");
        }

        var hasUsers = await _dbContext
            .Set<IdentityUserRole<Guid>>()
            .AsNoTracking()
            .AnyAsync(
                userRole => userRole.RoleId == role.Id,
                cancellationToken);

        if (hasUsers)
        {
            throw new InvalidOperationException(
                "This role cannot be deleted because users are assigned to it.");
        }

        var result = await _roleManager.DeleteAsync(role);

        EnsureIdentitySucceeded(
            result,
            "Role deletion failed");

        return true;
    }

    // ----------------------------------------------------
    // Get Users By Role
    // ----------------------------------------------------

    public async Task<PagedResult<RoleUserResponse>> GetUsersByRoleAsync(
        Guid roleId,
        RoleUserQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role ID is required.");
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateRoleUsersQueryParameters(parameters);

        var role = await _roleManager.FindByIdAsync(
            roleId.ToString());

        if (role is null)
        {
            throw new KeyNotFoundException(
                "Role was not found.");
        }

        var userRoles = _dbContext.Set<IdentityUserRole<Guid>>()
            .AsNoTracking();

        var query =
            from userRole in userRoles
            join user in _userManager.Users.AsNoTracking()
                on userRole.UserId equals user.Id
            where userRole.RoleId == roleId
            select user;

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(user =>
                user.FullName.ToLower().Contains(searchTerm) ||
                (user.Email != null &&
                 user.Email.ToLower().Contains(searchTerm)) ||
                (user.UserName != null &&
                 user.UserName.ToLower().Contains(searchTerm)));
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

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ProjectTo<RoleUserResponse>(
                _mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleUserResponse>
        {
            Items = users,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    // ----------------------------------------------------
    // Assign Role To User
    // ----------------------------------------------------

    public async Task<bool> AssignToUserAsync(
        AssignUserToRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty ||
            request.RoleId == Guid.Empty)
        {
            throw new ArgumentException(
                "UserId and RoleId are required.");
        }

        var user = await _userManager.FindByIdAsync(
            request.UserId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException(
                "User was not found.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "A role cannot be assigned to an inactive user.");
        }

        var role = await _roleManager.FindByIdAsync(
            request.RoleId.ToString());

        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            throw new KeyNotFoundException(
                "Role was not found.");
        }

        var alreadyAssigned = await _userManager.IsInRoleAsync(
            user,
            role.Name);

        if (alreadyAssigned)
        {
            return true;
        }

        var result = await _userManager.AddToRoleAsync(
            user,
            role.Name);

        EnsureIdentitySucceeded(
            result,
            "Assigning role to user failed");

        return true;
    }

    // ----------------------------------------------------
    // Remove Role From User
    // ----------------------------------------------------

    public async Task<bool> RemoveFromUserAsync(
        RemoveUserFromRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty ||
            request.RoleId == Guid.Empty)
        {
            throw new ArgumentException(
                "UserId and RoleId are required.");
        }

        var user = await _userManager.FindByIdAsync(
            request.UserId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException(
                "User was not found.");
        }

        var role = await _roleManager.FindByIdAsync(
            request.RoleId.ToString());

        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            throw new KeyNotFoundException(
                "Role was not found.");
        }

        var isInRole = await _userManager.IsInRoleAsync(
            user,
            role.Name);

        if (!isInRole)
        {
            throw new InvalidOperationException(
                "The user is not assigned to this role.");
        }

        if (string.Equals(
                role.Name,
                "Admin",
                StringComparison.OrdinalIgnoreCase))
        {
            var activeAdmins = await _userManager
                .GetUsersInRoleAsync(role.Name);

            var activeAdminCount = activeAdmins.Count(
                admin => admin.IsActive);

            if (user.IsActive && activeAdminCount <= 1)
            {
                throw new InvalidOperationException(
                    "The role cannot be removed from the last active administrator.");
            }
        }

        var result = await _userManager.RemoveFromRoleAsync(
            user,
            role.Name);

        EnsureIdentitySucceeded(
            result,
            "Removing role from user failed");

        return true;
    }

    // ----------------------------------------------------
    // Validation Helpers
    // ----------------------------------------------------

    private static string NormalizeRoleName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException(
                "Role name is required.");
        }

        var normalizedName = roleName.Trim();

        if (normalizedName.Length > 256)
        {
            throw new ArgumentException(
                "Role name cannot exceed 256 characters.");
        }

        return normalizedName;
    }

    private static bool IsSystemRole(string? roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) &&
               SystemRoles.Contains(roleName);
    }

    private static void ValidateRoleQueryParameters(
        RoleQueryParameters parameters)
    {
        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.MinUsersCount.HasValue &&
            parameters.MinUsersCount.Value < 0)
        {
            throw new ArgumentException(
                "MinUsersCount cannot be negative.");
        }

        if (parameters.MaxUsersCount.HasValue &&
            parameters.MaxUsersCount.Value < 0)
        {
            throw new ArgumentException(
                "MaxUsersCount cannot be negative.");
        }

        if (parameters.MinUsersCount.HasValue &&
            parameters.MaxUsersCount.HasValue &&
            parameters.MinUsersCount.Value >
            parameters.MaxUsersCount.Value)
        {
            throw new ArgumentException(
                "MinUsersCount cannot be greater than MaxUsersCount.");
        }
    }

    private static void ValidateRoleUsersQueryParameters(
        RoleUserQueryParameters parameters)
    {
        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }
    }

    private static void EnsureIdentitySucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"{operation}: {errors}");
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private RoleManagementResponse MapToResponse(
        IdentityRole<Guid> role,
        int usersCount)
    {
        var response = _mapper.Map<RoleManagementResponse>(role);

        response.UsersCount = usersCount;

        return response;
    }
}