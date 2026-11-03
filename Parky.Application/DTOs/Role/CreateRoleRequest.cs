using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.DTOs.Role;

public sealed class CreateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}