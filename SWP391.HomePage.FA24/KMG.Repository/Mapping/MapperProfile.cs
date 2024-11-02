using AutoMapper;
using KMG.Repository.Dto;
using KMG.Repository.Models;
using System;


public class MapperProfile : Profile
{
    public MapperProfile()
    {
        // Mapping from Consignment to ConsignmentDto and vice versa
        CreateMap<Consignment, ConsignmentDto>()
            .ForMember(dest => dest.ConsignmentDateFrom, opt => opt.MapFrom(src => src.ConsignmentDateFrom))
            .ReverseMap()
            .ForMember(dest => dest.ConsignmentDateTo, opt => opt.MapFrom(src => src.ConsignmentDateTo));


    }
}
