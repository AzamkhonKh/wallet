
using System.ComponentModel.DataAnnotations;

namespace WalletNet.DTOs
{
    public class RegisterDTO
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class VerifyOtpDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; }
    }

    public class ExternalAuthDTO
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        public string IdToken { get; set; }
    }

    public class AuthResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool RequiresOtp { get; set; }
    }

    public class RefreshTokenDTO
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
