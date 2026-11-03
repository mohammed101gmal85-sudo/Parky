using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.DTOs.Role;

public sealed class AssignUserToRoleRequest
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }
}
