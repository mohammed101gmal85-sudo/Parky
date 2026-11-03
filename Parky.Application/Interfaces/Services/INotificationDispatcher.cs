using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.Interfaces.Services;

public interface INotificationDispatcher
{
    Task DispatchAsync(
        Guid userId,
        string title,
        string message,
        string notificationType,
        string? referenceId = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);
}