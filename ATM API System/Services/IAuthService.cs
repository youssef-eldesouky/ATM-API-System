using ATM_API_System.Dtos;
using System.Threading.Tasks;
using ATM_API_System.DTOs;

namespace ATM_API_System.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> AuthenticateAsync(LoginRequestDto request);
        Task<bool> ChangePinAsync(int cardId, string currentPin, string newPin);
        Task<AuthResponseDto?> AuthenticateOperatorAsync(OperatorLoginDto request);
    }
}