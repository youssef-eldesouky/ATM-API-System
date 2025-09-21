using AutoMapper;
using ATM_API_System.DTOs;
using ATM_API_System.Data;
using ATM_API_System.Dtos;
using ATM_API_System.Models;

namespace ATM_API_System.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Transaction, TransactionResponseDto>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Id));

            CreateMap<Account, BalanceResponseDto>()
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.Id));
        }
    }
}