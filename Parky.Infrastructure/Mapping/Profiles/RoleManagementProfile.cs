using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Parky.Application.DTOs.Role;
using Parky.Infrastructure.Identity;

namespace Parky.Infrastructure.Mapping.Profiles;

public sealed class RoleManagementProfile : Profile
{
    public RoleManagementProfile()
    {
        CreateMap<IdentityRole<Guid>, RoleManagementResponse>()
            .ForMember(
                destination => destination.Name,
                options => options.MapFrom(
                    source => source.Name ?? string.Empty))
            .ForMember(
                destination => destination.NormalizedName,
                options => options.MapFrom(
                    source => source.NormalizedName ?? string.Empty))
            .ForMember(
                destination => destination.UsersCount,
                options => options.Ignore());

        CreateMap<ApplicationUser, RoleUserResponse>()
            .ForMember(
                destination => destination.Email,
                options => options.MapFrom(
                    source => source.Email ?? string.Empty));
    }
}
