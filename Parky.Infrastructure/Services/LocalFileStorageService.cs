using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Parky.Application.Common;
using Parky.Application.Interfaces.Services;

namespace Parky.Infrastructure.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly string _storageDirectory;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var webRootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;

        _storageDirectory = Path.Combine(
            webRootPath,
            "uploads",
            "parking-images");
    }

    public async Task<StoredFileResult> SaveImageAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The supplied image stream is not readable.",
                nameof(content));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new InvalidDataException(
                "Image content type is required.");
        }

        var header = new byte[12];
        var headerLength = 0;

        while (headerLength < header.Length)
        {
            var bytesRead = await content.ReadAsync(
                header.AsMemory(headerLength),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            headerLength += bytesRead;
        }

        var imageFormat = DetectImageFormat(
            header,
            headerLength);

        if (imageFormat is null)
        {
            throw new InvalidDataException(
                "Only valid JPEG, PNG, and WebP image signatures are supported.");
        }

        var normalizedContentType = contentType
            .Split(';', 2)[0]
            .Trim();

        if (!string.Equals(
                normalizedContentType,
                imageFormat.Value.ContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The supplied content type does not match the actual image format.");
        }

        Directory.CreateDirectory(_storageDirectory);

        var publicId = $"{Guid.NewGuid():N}{imageFormat.Value.Extension}";

        var filePath = Path.Combine(
            _storageDirectory,
            publicId);

        try
        {
            await using var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous);

            long totalBytes = headerLength;

            if (totalBytes > MaxFileSizeBytes)
            {
                throw new InvalidDataException(
                    "Image size cannot exceed 5 MB.");
            }

            await fileStream.WriteAsync(
                header.AsMemory(0, headerLength),
                cancellationToken);

            var buffer = new byte[81920];

            while (true)
            {
                var bytesRead = await content.ReadAsync(
                    buffer.AsMemory(),
                    cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;

                if (totalBytes > MaxFileSizeBytes)
                {
                    throw new InvalidDataException(
                        "Image size cannot exceed 5 MB.");
                }

                await fileStream.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }

            await fileStream.FlushAsync(cancellationToken);
        }
        catch
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }

            throw;
        }

        return new StoredFileResult
        {
            ImageUrl = $"/uploads/parking-images/{publicId}",
            PublicId = publicId
        };
    }

    public Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ArgumentException(
                "Public ID is required.",
                nameof(publicId));
        }

        if (publicId != Path.GetFileName(publicId) ||
            publicId.Contains('/') ||
            publicId.Contains('\\'))
        {
            throw new ArgumentException(
                "Invalid image public ID.",
                nameof(publicId));
        }

        var fileNameWithoutExtension =
            Path.GetFileNameWithoutExtension(publicId);

        if (!Guid.TryParseExact(
                fileNameWithoutExtension,
                "N",
                out _))
        {
            throw new ArgumentException(
                "Invalid image public ID.",
                nameof(publicId));
        }

        var extension = Path.GetExtension(publicId);

        if (extension is not ".jpg" and not ".png" and not ".webp")
        {
            throw new ArgumentException(
                "Unsupported image file extension.",
                nameof(publicId));
        }

        var filePath = Path.Combine(
            _storageDirectory,
            publicId);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static (string Extension, string ContentType)?
        DetectImageFormat(byte[] header, int length)
    {
        // JPEG
        if (length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return (".jpg", "image/jpeg");
        }

        // PNG
        if (length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return (".png", "image/png");
        }

        // WebP
        if (length >= 12 &&
            header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x57 &&
            header[9] == 0x45 &&
            header[10] == 0x42 &&
            header[11] == 0x50)
        {
            return (".webp", "image/webp");
        }

        return null;
    }
}