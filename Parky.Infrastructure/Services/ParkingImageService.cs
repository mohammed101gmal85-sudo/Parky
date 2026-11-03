using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingImage;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public sealed class ParkingImageService : IParkingImageService
{
    private readonly IParkingImageRepository _repository;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorageService;

    public ParkingImageService(
        IParkingImageRepository repository,
        IMapper mapper,
        IFileStorageService fileStorageService)
    {
        _repository = repository;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
    }

    // ----------------------------------------------------
    // Create Parking Image
    // ----------------------------------------------------

    public async Task<ParkingImageResponse> CreateAsync(
        CreateParkingImageRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidOwnerId(ownerId);

        if (request.ParkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        ValidateDisplayOrder(request.DisplayOrder);

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            request.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "The parking does not exist, is inactive, or does not belong to you.");
        }

        var orderExists = await _repository.DisplayOrderExistsAsync(
            request.ParkingId,
            request.DisplayOrder,
            cancellationToken: cancellationToken);

        if (orderExists)
        {
            throw new InvalidOperationException(
                "This display order is already used by another image in this parking.");
        }

        var storedFile = await _fileStorageService.SaveImageAsync(
            request.ImageContent,
            request.ContentType,
            cancellationToken);

        var image = new ParkingImage
        {
            ParkingId = request.ParkingId,
            ImageUrl = storedFile.ImageUrl,
            PublicId = storedFile.PublicId,
            AltText = NormalizeOptionalText(request.AltText),
            IsPrimary = request.IsPrimary,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _repository.AddAsync(
                image,
                cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedFileAsync(
                storedFile.PublicId);

            throw;
        }

        if (request.IsPrimary)
        {
            await ClearPrimaryImageAsync(
                request.ParkingId,
                image.Id,
                cancellationToken);
        }

        return MapToResponse(image);
    }

    // ----------------------------------------------------
    // Get Parking Image By Id
    // ----------------------------------------------------

    public async Task<ParkingImageResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var image = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        return image is null
            ? null
            : MapToResponse(image);
    }

    // ----------------------------------------------------
    // Get All Parking Images
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingImageResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var images = await _repository.GetAllAsync(
            cancellationToken);

        return _mapper.Map<List<ParkingImageResponse>>(images);
    }

    // ----------------------------------------------------
    // Get All Parking Images With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingImageResponse>> GetAllAsync(
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _repository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Parking Images By Parking Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingImageResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        var parkingExists = await _repository.ParkingExistsAsync(
            parkingId,
            cancellationToken);

        if (!parkingExists)
        {
            return Array.Empty<ParkingImageResponse>();
        }

        var images = await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        return _mapper.Map<List<ParkingImageResponse>>(images);
    }

    // ----------------------------------------------------
    // Get Parking Images By Parking Id With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingImageResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.",
                nameof(parkingId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var parkingExists = await _repository.ParkingExistsAsync(
            parkingId,
            cancellationToken);

        if (!parkingExists)
        {
            return CreateEmptyPagedResponse(parameters);
        }

        var result = await _repository.GetPagedByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Update Parking Image
    // ----------------------------------------------------

    public async Task<ParkingImageResponse?> UpdateAsync(
        Guid id,
        UpdateParkingImageRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidOwnerId(ownerId);

        if (request.ParkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        ValidateDisplayOrder(request.DisplayOrder);

        var image = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (image is null)
        {
            return null;
        }

        var ownsCurrentParking =
            await _repository.ParkingBelongsToOwnerAsync(
                image.ParkingId,
                ownerId,
                cancellationToken);

        if (!ownsCurrentParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this image.");
        }

        var ownsTargetParking =
            await _repository.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                cancellationToken);

        if (!ownsTargetParking)
        {
            throw new InvalidOperationException(
                "The target parking does not exist, is inactive, or does not belong to you.");
        }

        var orderExists = await _repository.DisplayOrderExistsAsync(
            request.ParkingId,
            request.DisplayOrder,
            id,
            cancellationToken);

        if (orderExists)
        {
            throw new InvalidOperationException(
                "This display order is already used by another image in the target parking.");
        }

        var storedFile = await _fileStorageService.SaveImageAsync(
            request.ImageContent,
            request.ContentType,
            cancellationToken);

        var oldPublicId = image.PublicId;

        image.ParkingId = request.ParkingId;
        image.ImageUrl = storedFile.ImageUrl;
        image.PublicId = storedFile.PublicId;
        image.AltText = NormalizeOptionalText(request.AltText);
        image.IsPrimary = request.IsPrimary;
        image.DisplayOrder = request.DisplayOrder;

        try
        {
            await _repository.UpdateAsync(
                image,
                cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedFileAsync(
                storedFile.PublicId);

            throw;
        }

        if (request.IsPrimary)
        {
            await ClearPrimaryImageAsync(
                request.ParkingId,
                image.Id,
                cancellationToken);
        }

        await DeleteStoredImageIfLocalAsync(
            oldPublicId,
            cancellationToken);

        return MapToResponse(image);
    }

    // ----------------------------------------------------
    // Delete Parking Image
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidOwnerId(ownerId);

        var image = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (image is null)
        {
            return false;
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            image.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this image.");
        }

        var wasPrimary = image.IsPrimary;
        var parkingId = image.ParkingId;
        var publicId = image.PublicId;

        await _repository.DeleteAsync(
            image,
            cancellationToken);

        if (wasPrimary)
        {
            var remainingImages = await _repository.GetByParkingIdAsync(
                parkingId,
                cancellationToken);

            var nextPrimary = remainingImages
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.CreatedAt)
                .ThenBy(image => image.Id)
                .FirstOrDefault();

            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;

                await _repository.UpdateAsync(
                    nextPrimary,
                    cancellationToken);
            }
        }

        await DeleteStoredImageIfLocalAsync(
            publicId,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Update Primary Image Status
    // ----------------------------------------------------

    public async Task<ParkingImageResponse?> UpdatePrimaryStatusAsync(
        Guid id,
        UpdateParkingImagePrimaryRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidOwnerId(ownerId);

        var image = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (image is null)
        {
            return null;
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            image.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this image.");
        }

        if (request.IsPrimary)
        {
            await ClearPrimaryImageAsync(
                image.ParkingId,
                image.Id,
                cancellationToken);
        }

        image.IsPrimary = request.IsPrimary;

        await _repository.UpdateAsync(
            image,
            cancellationToken);

        return MapToResponse(image);
    }

    // ----------------------------------------------------
    // Reorder Parking Images
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingImageResponse>> ReorderAsync(
        Guid parkingId,
        IReadOnlyList<Guid> imageIds,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidOwnerId(ownerId);

        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.",
                nameof(parkingId));
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            parkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "The parking does not exist, is inactive, or does not belong to you.");
        }

        if (imageIds is null || imageIds.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one image ID must be provided.");
        }

        if (imageIds.Any(id => id == Guid.Empty))
        {
            throw new InvalidOperationException(
                "The image ID list contains an invalid ID.");
        }

        if (imageIds.Distinct().Count() != imageIds.Count)
        {
            throw new InvalidOperationException(
                "The image ID list contains duplicates.");
        }

        var images = (await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken)).ToList();

        if (images.Count != imageIds.Count ||
            !images.Select(image => image.Id)
                .ToHashSet()
                .SetEquals(imageIds))
        {
            throw new InvalidOperationException(
                "The list must contain every image belonging to this parking exactly once.");
        }

        var imagesById = images.ToDictionary(image => image.Id);

        for (var index = 0; index < imageIds.Count; index++)
        {
            var image = imagesById[imageIds[index]];

            image.DisplayOrder = index;

            await _repository.UpdateAsync(
                image,
                cancellationToken);
        }

        var reorderedImages = await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        return _mapper.Map<List<ParkingImageResponse>>(
            reorderedImages);
    }

    // ----------------------------------------------------
    // Clear Previous Primary Image
    // ----------------------------------------------------

    private async Task ClearPrimaryImageAsync(
        Guid parkingId,
        Guid? excludedImageId,
        CancellationToken cancellationToken)
    {
        var images = await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        var primaryImages = images
            .Where(image =>
                image.IsPrimary &&
                image.Id != excludedImageId)
            .ToList();

        foreach (var primaryImage in primaryImages)
        {
            primaryImage.IsPrimary = false;

            await _repository.UpdateAsync(
                primaryImage,
                cancellationToken);
        }
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        ParkingImageQueryParameters parameters)
    {
        if (parameters.ParkingId.HasValue &&
            parameters.ParkingId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is invalid.");
        }

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.MinDisplayOrder.HasValue &&
            parameters.MinDisplayOrder.Value < 0)
        {
            throw new ArgumentException(
                "MinDisplayOrder cannot be negative.");
        }

        if (parameters.MaxDisplayOrder.HasValue &&
            parameters.MaxDisplayOrder.Value < 0)
        {
            throw new ArgumentException(
                "MaxDisplayOrder cannot be negative.");
        }

        if (parameters.MinDisplayOrder.HasValue &&
            parameters.MaxDisplayOrder.HasValue &&
            parameters.MinDisplayOrder.Value >
            parameters.MaxDisplayOrder.Value)
        {
            throw new ArgumentException(
                "MinDisplayOrder cannot be greater than MaxDisplayOrder.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }
    }

    // ----------------------------------------------------
    // Validate Display Order
    // ----------------------------------------------------

    private static void ValidateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0 || displayOrder > 10000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "DisplayOrder must be between 0 and 10000.");
        }
    }

    // ----------------------------------------------------
    // Owner Validation
    // ----------------------------------------------------

    private static void EnsureValidOwnerId(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID is required.",
                nameof(ownerId));
        }
    }

    // ----------------------------------------------------
    // Delete Uploaded File After Database Failure
    // ----------------------------------------------------

    private async Task TryDeleteUploadedFileAsync(string publicId)
    {
        try
        {
            await _fileStorageService.DeleteImageAsync(
                publicId,
                CancellationToken.None);
        }
        catch
        {
        }
    }

    // ----------------------------------------------------
    // Delete Local Stored Image
    // ----------------------------------------------------

    private async Task DeleteStoredImageIfLocalAsync(
        string? publicId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var fileName = Path.GetFileName(publicId);

        var isSafeFileName = string.Equals(
            fileName,
            publicId,
            StringComparison.Ordinal);

        var isValidGuid = Guid.TryParseExact(
            Path.GetFileNameWithoutExtension(publicId),
            "N",
            out _);

        var extension = Path.GetExtension(publicId);

        var isAllowedExtension =
            extension is ".jpg" or ".png" or ".webp";

        if (!isSafeFileName ||
            !isValidGuid ||
            !isAllowedExtension)
        {
            return;
        }

        await _fileStorageService.DeleteImageAsync(
            publicId,
            cancellationToken);
    }

    // ----------------------------------------------------
    // Normalize Optional Text
    // ----------------------------------------------------

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private ParkingImageResponse MapToResponse(ParkingImage image)
    {
        return _mapper.Map<ParkingImageResponse>(image);
    }

    private PagedResult<ParkingImageResponse> MapToPagedResponse(
        PagedResult<ParkingImage> result)
    {
        return new PagedResult<ParkingImageResponse>
        {
            Items = _mapper.Map<List<ParkingImageResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    // ----------------------------------------------------
    // Empty Paged Result
    // ----------------------------------------------------

    private static PagedResult<ParkingImageResponse> CreateEmptyPagedResponse(
        ParkingImageQueryParameters parameters)
    {
        return new PagedResult<ParkingImageResponse>
        {
            Items = new List<ParkingImageResponse>(),
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = 0
        };
    }
}