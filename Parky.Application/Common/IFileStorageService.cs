using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveImageAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default);
}