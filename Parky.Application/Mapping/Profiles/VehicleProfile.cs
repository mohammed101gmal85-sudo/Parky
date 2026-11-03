using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Parky.Application.DTOs.Vehicle;
using Parky.Domain.Entities;

namespace Parky.Application.Mapping.Profiles;

public sealed class VehicleProfile : Profile
{
    public VehicleProfile()
    {
        CreateMap<Vehicle, VehicleResponse>();
    }
}