using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Parky.Domain.Entities;
using Parky.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Infrastructure.Persistence;

public class ParkyDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ParkyDbContext(
        DbContextOptions<ParkyDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Parking> Parkings => Set<Parking>();
    public DbSet<ParkingFloor> ParkingFloors => Set<ParkingFloor>();
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingGate> ParkingGates => Set<ParkingGate>();
    public DbSet<ParkingDevice> ParkingDevices => Set<ParkingDevice>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationSpot> ReservationSpots => Set<ReservationSpot>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<QRCode> QRCodes => Set<QRCode>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ParkingImage> ParkingImages => Set<ParkingImage>();
    public DbSet<ParkingOperatingHour> ParkingOperatingHours =>
        Set<ParkingOperatingHour>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<ParkingSpotType> ParkingSpotTypes =>
        Set<ParkingSpotType>();
    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.ProfileImageUrl)
                .HasMaxLength(2048);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });

        builder.Entity<Parking>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Governorate)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Address)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ParkingFloor>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(x => x.Parking)
                .WithMany(x => x.Floors)
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingId, x.FloorNumber })
                .IsUnique();
        });

        builder.Entity<ParkingSpot>(entity =>
        {
            entity.Property(x => x.SpotNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.XCoordinate).HasPrecision(10, 2);
            entity.Property(x => x.YCoordinate).HasPrecision(10, 2);
            entity.Property(x => x.Width).HasPrecision(10, 2);
            entity.Property(x => x.Height).HasPrecision(10, 2);

            entity.HasOne(x => x.ParkingFloor)
                .WithMany(x => x.Spots)
                .HasForeignKey(x => x.ParkingFloorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ParkingSpotType)
                .WithMany(x => x.ParkingSpots)
                .HasForeignKey(x => x.ParkingSpotTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingFloorId, x.SpotNumber })
                .IsUnique();
        });

        builder.Entity<ParkingGate>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.GateCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.GateType).HasMaxLength(50).IsRequired();

            entity.HasOne(x => x.Parking)
                .WithMany()
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingId, x.GateCode })
                .IsUnique();
        });

        builder.Entity<ParkingDevice>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.DeviceCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DeviceType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(150);
            entity.Property(x => x.IpAddress).HasMaxLength(45);

            entity.HasOne(x => x.ParkingGate)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.ParkingGateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.DeviceCode).IsUnique();
        });

        builder.Entity<Reservation>(entity =>
        {
            entity.Property(x => x.ReservationNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.FinalAmount).HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.CancellationReason).HasMaxLength(500);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Parking>()
                .WithMany()
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Vehicle>()
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ReservationNumber).IsUnique();
            entity.HasIndex(x => new { x.ParkingId, x.StartTime, x.EndTime });
        });

        builder.Entity<ReservationSpot>(entity =>
        {
            entity.Property(x => x.PricePerHour).HasPrecision(18, 2);
            entity.Property(x => x.ReservedAmount).HasPrecision(18, 2);

            entity.HasOne(x => x.Reservation)
                .WithMany(x => x.ReservationSpots)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ParkingSpot)
                .WithMany()
                .HasForeignKey(x => x.ParkingSpotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ReservationId, x.ParkingSpotId })
                .IsUnique();
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.PaymentNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.RefundedAmount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PaymentStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PaymentProvider).HasMaxLength(100);
            entity.Property(x => x.ProviderTransactionId).HasMaxLength(200);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
            entity.Property(x => x.FailureReason).HasMaxLength(1000);

            entity.HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.PaymentNumber).IsUnique();
            entity.HasIndex(x => x.ProviderTransactionId);
            entity.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.Property(x => x.InvoiceNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.InvoiceStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);

            entity.HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
        });

        builder.Entity<QRCode>(entity =>
        {
            entity.Property(x => x.TokenHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TokenHash).IsUnique();
        });

        builder.Entity<Vehicle>(entity =>
        {
            entity.Property(x => x.PlateNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.PlateCode).HasMaxLength(20);
            entity.Property(x => x.Make).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.Property(x => x.VehicleType).HasMaxLength(50).IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.PlateNumber).IsUnique();
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.NotificationType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReferenceId).HasMaxLength(100);
            entity.Property(x => x.ActionUrl).HasMaxLength(2048);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Parking)
                .WithMany()
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.UserId, x.ParkingId })
                .IsUnique();
        });

        builder.Entity<ParkingImage>(entity =>
        {
            entity.Property(x => x.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.PublicId).HasMaxLength(300);
            entity.Property(x => x.AltText).HasMaxLength(250);

            entity.HasOne(x => x.Parking)
                .WithMany()
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingId, x.DisplayOrder });
        });

        builder.Entity<ParkingOperatingHour>(entity =>
        {
            entity.HasOne(x => x.Parking)
                .WithMany()
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingId, x.DayOfWeek })
                .IsUnique();
        });

        builder.Entity<PricingRule>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PricingType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PricePerHour).HasPrecision(18, 2);
            entity.Property(x => x.FlatPrice).HasPrecision(18, 2);
            entity.Property(x => x.MaximumDailyPrice).HasPrecision(18, 2);

            entity.HasOne(x => x.Parking)
                .WithMany()
                .HasForeignKey(x => x.ParkingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingId, x.IsActive, x.Priority });
        });

        builder.Entity<ParkingSpotType>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.AdditionalHourlyRate).HasPrecision(18, 2);

            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<DeviceEvent>(entity =>
        {
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EventStatus).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Payload).HasMaxLength(4000);

            entity.HasOne(x => x.ParkingDevice)
                .WithMany(x => x.DeviceEvents)
                .HasForeignKey(x => x.ParkingDeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParkingDeviceId, x.OccurredAt });
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Message)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(x => x.NotificationType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ReferenceId)
                .HasMaxLength(100);

            entity.Property(x => x.ActionUrl)
                .HasMaxLength(500);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.CreatedAt
            });

            entity.HasIndex(x => new
            {
                x.UserId,
                x.IsRead,
                x.CreatedAt
            });
        });

       
    }
}

