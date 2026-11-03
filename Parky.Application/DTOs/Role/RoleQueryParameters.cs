using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.Role;

public sealed class RoleQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public int? MinUsersCount { get; set; }

    public int? MaxUsersCount { get; set; }
}