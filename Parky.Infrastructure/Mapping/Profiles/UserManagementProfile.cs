using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Parky.Application.DTOs.User;
using Parky.Infrastructure.Identity;

namespace Parky.Infrastructure.Mapping.Profiles;

public sealed class UserManagementProfile : Profile
{
    public UserManagementProfile()
    {
        CreateMap<ApplicationUser, UserManagementResponse>()
            .ForMember(
                destination => destination.Email,
                options => options.MapFrom(
                    source => source.Email ?? string.Empty))
            .ForMember(
                destination => destination.Roles,
                options => options.Ignore());
    }
}
