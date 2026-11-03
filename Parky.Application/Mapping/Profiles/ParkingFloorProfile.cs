using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Parky.Application.DTOs.ParkingFloor;
using Parky.Domain.Entities;

namespace Parky.Application.Mapping.Profiles;

public sealed class ParkingFloorProfile : Profile
{
    public ParkingFloorProfile()
    {
        CreateMap<ParkingFloor, ParkingFloorResponse>();
    }
}