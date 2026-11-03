using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.DTOs.Auth;

namespace Parky.Application.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(TokenUserInfo user);
}

