using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Parky.API.Controllers;
using Xunit;

namespace Parky.Tests.Authorization;

public class AuthorizationAttributeTests
{
    [Fact]
    public void NotificationController_ShouldRequireAuthentication()
    {
        var attribute = typeof(NotificationController)
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(NotificationController.GetAll))]
    [InlineData(nameof(NotificationController.GetById))]
    [InlineData(nameof(NotificationController.Create))]
    [InlineData(nameof(NotificationController.Update))]
    [InlineData(nameof(NotificationController.DeleteByAdmin))]
    public void NotificationAdminEndpoints_ShouldRequireAdminRole(
        string methodName)
    {
        var method = typeof(NotificationController)
            .GetMethod(methodName);

        method.Should().NotBeNull();

        var attribute = method!
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void ReservationController_ShouldRequireAuthentication()
    {
        var attribute = typeof(ReservationController)
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(ReservationController.GetById))]
    [InlineData(nameof(ReservationController.GetAll))]
    [InlineData(nameof(ReservationController.GetByParking))]
    public void ReservationOwnerEndpoints_ShouldRequireOwnerRole(
        string methodName)
    {
        var method = typeof(ReservationController)
            .GetMethod(methodName);

        method.Should().NotBeNull();

        var attribute = method!
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be("Owner");
    }
}
