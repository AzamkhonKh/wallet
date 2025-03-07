using WalletNet.DTOs;
using System.Threading.Tasks;

namespace WalletNet.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterAsync(RegisterDTO model);
        Task<AuthResponseDTO> LoginAsync(LoginDTO model);
        Task<AuthResponseDTO> VerifyOtpAsync(VerifyOtpDTO model);
        Task<AuthResponseDTO> GoogleLoginAsync(ExternalAuthDTO model);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO model);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> SendOtpAsync(string email);
    }
}
