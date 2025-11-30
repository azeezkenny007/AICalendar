using AICalendar.Application.Calendar.DTOs;
using AICalendar.Domain.Entities;
using AutoMapper;

namespace AICalendar.Application.Calendar.MappingProfiles;

public class CalendarMappingProfile : Profile
{
    public CalendarMappingProfile()
    {
        // Transaction to TransactionDetailDto
        CreateMap<Transaction, TransactionDetailDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.Value))
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.PredictionStatus, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.PredictedNextDate, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.Confidence, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.AIExplanation, opt => opt.Ignore()); // Set manually in handler
    }
}
