using AutoMapper;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Service.DTOs.Registration;
using KoperasiTentera.Service.Responses.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.Mapping
{
    public class RegistrationMappingProfile : Profile
    {
        public RegistrationMappingProfile()
        {
            CreateMap<StartRegistrationRequest, Registrations>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedAtUtc, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedBy, opt => opt.Ignore());

            CreateMap<Registrations, RegistrationResponse>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.NextStep, opt => opt.Ignore())
                .ForMember(dest => dest.MaskedMobile, opt => opt.Ignore())
                .ForMember(dest => dest.MaskedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.Message, opt => opt.Ignore());
        }
    }
}
