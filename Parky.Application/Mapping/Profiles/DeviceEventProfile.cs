using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Parky.Application.DTOs.DeviceEvent;
using Parky.Domain.Entities;

namespace Parky.Application.Mapping.Profiles;

public sealed class DeviceEventProfile : Profile
{
    public DeviceEventProfile()
    {
        CreateMap<DeviceEvent, DeviceEventResponse>();
    }
}