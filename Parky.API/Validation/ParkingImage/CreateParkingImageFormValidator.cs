using FluentValidation;
using Parky.API.Models.ParkingImage;

namespace Parky.API.Validation.ParkingImage;

public sealed class CreateParkingImageFormValidator
    : AbstractValidator<CreateParkingImageForm>
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    public CreateParkingImageFormValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");

        RuleFor(x => x.Image)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Image file is required.")
            .Must(file => file!.Length > 0)
            .WithMessage("Image file cannot be empty.")
            .Must(file => file!.Length <= MaxFileSize)
            .WithMessage("Image size cannot exceed 5 MB.");

        RuleFor(x => x.AltText)
            .MaximumLength(250);

        RuleFor(x => x.DisplayOrder)
            .InclusiveBetween(0, 10000)
            .WithMessage("Display order must be between 0 and 10000.");
    }
}