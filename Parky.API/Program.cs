using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Parky.API.Filters;
using Parky.API.Middlewares;
using Parky.Application.Common;
using Parky.Application.Configuration;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Application.Mapping.Profiles;
using Parky.Infrastructure.Data.Seeding;
using Parky.Infrastructure.Identity;
using Parky.Infrastructure.Mapping.Profiles;
using Parky.Infrastructure.Persistence;
using Parky.Infrastructure.Repositories;
using Parky.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.API.Hubs;
using Parky.API.Services;
using System.Diagnostics;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Parky.API.Exceptions;
using Parky.API.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Parky.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    var logDirectory = Path.Combine(
        context.HostingEnvironment.ContentRootPath,
        "Logs");

    Directory.CreateDirectory(logDirectory);

    loggerConfiguration
        .MinimumLevel.Is(
            context.HostingEnvironment.IsDevelopment()
                ? LogEventLevel.Debug
                : LogEventLevel.Information)

        .MinimumLevel.Override(
            "Microsoft.AspNetCore",
            LogEventLevel.Warning)

        .MinimumLevel.Override(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogEventLevel.Warning)

        .Enrich.FromLogContext()

        .Enrich.WithProperty(
            "Application",
            "Parky.API")

        .Enrich.WithProperty(
            "Environment",
            context.HostingEnvironment.EnvironmentName)

        .WriteTo.Console(
            new CompactJsonFormatter())

        .WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: Path.Combine(
                logDirectory,
                "parky-.clef"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            shared: true);
});

var webRootPath = builder.Environment.WebRootPath;

if (string.IsNullOrWhiteSpace(webRootPath))
{
    webRootPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "wwwroot");
}

Directory.CreateDirectory(webRootPath);

// 1. Register Controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new FluentValidationActionFilter());
});

builder.Services.AddAutoMapper(
    cfg => { },
    typeof(ParkingProfile),
    typeof(RoleManagementProfile),
    typeof(UserManagementProfile));

builder.Services.AddValidatorsFromAssembly(
    typeof(ParkingProfile).Assembly,
    ServiceLifetime.Transient);


builder.Services.AddValidatorsFromAssembly(
    typeof(Parky.API.Validation.ParkingImage.CreateParkingImageFormValidator).Assembly,
    ServiceLifetime.Transient);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(modelState => modelState.Value?.Errors.Count > 0)
            .ToDictionary(
                modelState => modelState.Key,
                modelState => modelState.Value!.Errors
                    .Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage)
                    .ToArray());

        var response = new ApiResponse<object>(
            false,
            "Validation failed.",
            errors);

        return new BadRequestObjectResult(response);
    };
});


// 2. Register Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Register Entity Framework Core and SQL Server
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");
}

builder.Services.AddDbContext<ParkyDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register TimeProvider required by Identity services
builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);

// Register Data Protection services
builder.Services.AddDataProtection();

// 4. Register ASP.NET Core Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ParkyDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// 5. Register Authorization services
var jwtSection = builder.Configuration.GetSection("Jwt");

var issuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer is missing.");

var audience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("JWT Audience is missing.");

var secretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is missing.");

if (Encoding.UTF8.GetByteCount(secretKey) < 32)
{
    throw new InvalidOperationException(
        "JWT SecretKey must contain at least 32 bytes.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var requestPath = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    requestPath.StartsWithSegments(
                        NotificationHub.Route))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Issuer),
        "JWT Issuer is required.")
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.Audience),
        "JWT Audience is required.")
    .Validate(
        settings => !string.IsNullOrWhiteSpace(settings.SecretKey)
            && Encoding.UTF8.GetByteCount(settings.SecretKey) >= 32,
        "JWT SecretKey must contain at least 32 bytes.")
    .Validate(
        settings => settings.AccessTokenExpirationMinutes > 0,
        "JWT access token expiration must be greater than zero.")
    .Validate(
        settings => settings.RefreshTokenExpirationDays > 0,
        "JWT refresh token expiration must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddHealthChecks()

    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(
            "Parky API is running."),
        tags: new[] { "live" })

    .AddCheck<SqlServerHealthCheck>(
        "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "database" });

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthorization();
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddScoped<IRefreshTokenRepository,RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRoleManagementService,RoleManagementService>();
builder.Services.AddScoped<IParkingRepository, ParkingRepository>();
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddScoped<IParkingDeviceRepository,ParkingDeviceRepository>();
builder.Services.AddScoped<IParkingDeviceService,ParkingDeviceService>();
builder.Services.AddScoped<IParkingFloorRepository, ParkingFloorRepository>();
builder.Services.AddScoped<IParkingFloorService, ParkingFloorService>();
builder.Services.AddScoped<IParkingGateRepository, ParkingGateRepository>();
builder.Services.AddScoped<IParkingGateService, ParkingGateService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IParkingImageRepository, ParkingImageRepository>();
builder.Services.AddScoped<IParkingImageService, ParkingImageService>();
builder.Services.AddScoped<IParkingOperatingHourRepository,ParkingOperatingHourRepository>();
builder.Services.AddScoped<IParkingOperatingHourService,ParkingOperatingHourService>();
builder.Services.AddScoped<IParkingSpotRepository,ParkingSpotRepository>();
builder.Services.AddScoped<IParkingSpotService,ParkingSpotService>();
builder.Services.AddScoped<IParkingSpotTypeRepository,ParkingSpotTypeRepository>();
builder.Services.AddScoped<IParkingSpotTypeService,ParkingSpotTypeService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPricingRuleRepository, PricingRuleRepository>();
builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();
builder.Services.AddScoped<IQRCodeRepository, QRCodeRepository>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationSpotRepository,ReservationSpotRepository>();
builder.Services.AddScoped<IReservationSpotService,ReservationSpotService>();
builder.Services.AddScoped<IUserVehicleRepository,UserVehicleRepository>();
builder.Services.AddScoped<IUserVehicleService,UserVehicleService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IDeviceEventRepository,DeviceEventRepository>();
builder.Services.AddScoped<IDeviceEventService, DeviceEventService>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddScoped<INotificationDispatcher,NotificationDispatcher>();
builder.Services.AddSignalR();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

//
var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.GetLevel = (httpContext, elapsed, exception) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Verbose;
        }

        if (exception is not null ||
            httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= 400)
        {
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set(
            "RequestHost",
            httpContext.Request.Host.Value);

        diagnosticContext.Set(
            "RequestScheme",
            httpContext.Request.Scheme);

        diagnosticContext.Set(
            "TraceId",
            Activity.Current?.TraceId.ToString()
                ?? httpContext.TraceIdentifier);
    };
});

app.UseExceptionHandler();

app.UseMiddleware<GlobalExceptionMiddleware>();

// 6. Configure HTTP request pipeline
app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.Append(
            "X-Content-Type-Options",
            "nosniff");
    }
});

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

var healthStatusCodes = new Dictionary<HealthStatus, int>
{
    [HealthStatus.Healthy] = StatusCodes.Status200OK,

    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,

    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
};

app.MapHealthChecks(
    "/health/live",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),

        ResponseWriter = HealthCheckResponseWriter.WriteResponse,

        ResultStatusCodes = healthStatusCodes,

        AllowCachingResponses = false
    });

app.MapHealthChecks(
    "/health/ready",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),

        ResponseWriter = HealthCheckResponseWriter.WriteResponse,

        ResultStatusCodes = healthStatusCodes,

        AllowCachingResponses = false
    });


app.MapHub<NotificationHub>(
    NotificationHub.Route)
    .RequireAuthorization();

// Seed Identity roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await IdentitySeeder.SeedRolesAsync(roleManager);
}

app.Run();

