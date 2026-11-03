using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Application.DTOs.Auth;

public sealed class TokenUserInfo
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; init; }
        = Array.Empty<string>();
}

