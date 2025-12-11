using AICalendar.Domain.Entities;
using AutoMapper;

namespace AICalendar.Application.Users;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));
    }
}
