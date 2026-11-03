
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Auth;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(
                request,
                cancellationToken);

            var response = ApiResponse<AuthResponse>.SuccessResponse(
                result,
                "Registration completed successfully.");

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                "Registration failed: {Reason}",
                exception.Message);

            return BadRequest(
                ApiResponse<AuthResponse>.FailureResponse(
                    exception.Message));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(
                request,
                cancellationToken);

            return Ok(
                ApiResponse<AuthResponse>.SuccessResponse(
                    result,
                    "Login successful."));
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(
                ApiResponse<AuthResponse>.FailureResponse(
                    "Invalid email or password."));
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);

            return Ok(
                ApiResponse<AuthResponse>.SuccessResponse(
                    result,
                    "Token refreshed successfully."));
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(
                ApiResponse<AuthResponse>.FailureResponse(
                    "The refresh token is invalid or expired."));
        }
    }

    [HttpPost("revoke")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> Revoke(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _authService.RevokeRefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);

            return Ok(
                ApiResponse<bool>.SuccessResponse(
                    true,
                    "Refresh token revoked successfully."));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(
                ApiResponse<bool>.FailureResponse(
                    "The refresh token could not be revoked."));
        }
    }
}


