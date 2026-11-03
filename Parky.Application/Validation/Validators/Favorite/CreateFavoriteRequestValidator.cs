using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Favorite;

namespace Parky.Application.Validation.Validators.Favorite;

public sealed class CreateFavoriteRequestValidator
    : AbstractValidator<CreateFavoriteRequest>
{
    public CreateFavoriteRequestValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");
    }
}
