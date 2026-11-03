//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using Parky.Application.Configuration;
//using Parky.Application.DTOs.Auth;
//using Parky.Application.Interfaces.Services;

//namespace Parky.Infrastructure.Services;

//public sealed class JwtTokenService : IJwtTokenService
//{
//    private readonly JwtSettings _jwtSettings;

//    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
//    {
//        _jwtSettings = jwtOptions.Value;
//    }

//    public string GenerateAccessToken(TokenUserInfo user)
//    {
//        var secretKey = Encoding.UTF8.GetBytes(
//            _jwtSettings.SecretKey);

//        var signingKey = new SymmetricSecurityKey(secretKey);

//        var credentials = new SigningCredentials(
//            signingKey,
//            SecurityAlgorithms.HmacSha256);

//        var claims = new List<Claim>
//        {
//            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
//            new(JwtRegisteredClaimNames.Email, user.Email),
//            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
//            new(ClaimTypes.Name, user.FullName)
//        };

//        foreach (var role in user.Roles.Distinct())
//        {
//            claims.Add(new Claim(ClaimTypes.Role, role));
//        }

//        var expiresAt = DateTime.UtcNow.AddMinutes(
//            _jwtSettings.AccessTokenExpirationMinutes);

//        var token = new JwtSecurityToken(
//            issuer: _jwtSettings.Issuer,
//            audience: _jwtSettings.Audience,
//            claims: claims,
//            notBefore: DateTime.UtcNow,
//            expires: expiresAt,
//            signingCredentials: credentials);

//        return new JwtSecurityTokenHandler().WriteToken(token);
//    }
//}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Parky.Application.Configuration;
using Parky.Application.DTOs.Auth;
using Parky.Application.Interfaces.Services;

namespace Parky.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(
        IOptions<JwtSettings> jwtOptions,
        TimeProvider timeProvider)
    {
        _jwtSettings = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public string GenerateAccessToken(TokenUserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(user));
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new ArgumentException(
                "User email is required.",
                nameof(user));
        }

        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(
            _jwtSettings.AccessTokenExpirationMinutes);

        var secretKey = Encoding.UTF8.GetBytes(
            _jwtSettings.SecretKey);

        var signingKey = new SymmetricSecurityKey(secretKey);

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new(
                ClaimTypes.Name,
                user.FullName ?? string.Empty)
        };

        foreach (var role in user.Roles
                     .Where(role => !string.IsNullOrWhiteSpace(role))
                     .Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
