using WalletNet.DTOs;
using WalletNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WalletNet.Data;
using Google.Apis.Auth;

namespace WalletNet.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<User> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO model)
        {
            // Check if email exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "Email already exists."
                };
            }

            // Create new user
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // Send OTP for email verification
            await SendOtpAsync(model.Email);

            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Registration successful. Please verify your email with the OTP sent.",
                RequiresOtp = true
            };
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "Invalid credentials."
                };
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "Invalid credentials."
                };
            }

            // If user's email is not verified, send OTP and require verification
            if (!user.IsEmailVerified)
            {
                await SendOtpAsync(user.Email);
                return new AuthResponseDTO
                {
                    IsSuccess = true,
                    Message = "Please verify your email with the OTP sent.",
                    RequiresOtp = true
                };
            }

            // Generate JWT and refresh token
            var tokens = await GenerateTokensAsync(user);

            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Login successful.",
                Token = tokens.Item1,
                RefreshToken = tokens.Item2,
                RequiresOtp = false
            };
        }

        public async Task<AuthResponseDTO> VerifyOtpAsync(VerifyOtpDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "User not found."
                };
            }

            // Check if OTP is valid and not expired
            if (user.OtpCode != model.OtpCode || user.OtpExpiration < DateTime.UtcNow)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "Invalid or expired OTP."
                };
            }

            // Mark email as verified
            user.IsEmailVerified = true;
            user.OtpCode = null;
            user.OtpExpiration = null;
            await _userManager.UpdateAsync(user);

            // Generate tokens
            var tokens = await GenerateTokensAsync(user);

            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Email verified successfully.",
                Token = tokens.Item1,
                RefreshToken = tokens.Item2,
                RequiresOtp = false
            };
        }

        public async Task<AuthResponseDTO> GoogleLoginAsync(ExternalAuthDTO model)
        {
            try
            {
                // Verify the Google token
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _configuration["Authentication:Google:ClientId"] }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(model.IdToken, settings);

                // Check if the user exists
                var user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    // Create a new user
                    user = new User
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        IsEmailVerified = true // Email is already verified by Google
                    };

                    var password = GenerateRandomPassword();
                    var result = await _userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        return new AuthResponseDTO
                        {
                            IsSuccess = false,
                            Message = "Failed to create user account."
                        };
                    }
                }

                // Generate JWT and refresh token
                var tokens = await GenerateTokensAsync(user);

                return new AuthResponseDTO
                {
                    IsSuccess = true,
                    Message = "Google authentication successful.",
                    Token = tokens.Item1,
                    RefreshToken = tokens.Item2,
                    RequiresOtp = false
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = $"Google authentication failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO model)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == model.RefreshToken);

            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.Expires < DateTime.UtcNow)
            {
                return new AuthResponseDTO
                {
                    IsSuccess = false,
                    Message = "Invalid or expired refresh token."
                };
            }

            var user = refreshToken.User;
            var tokens = await GenerateTokensAsync(user);

            // Revoke current refresh token
            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                IsSuccess = true,
                Message = "Token refreshed successfully.",
                Token = tokens.Item1,
                RefreshToken = tokens.Item2,
                RequiresOtp = false
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (token == null)
            {
                return false;
            }

            token.IsRevoked = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendOtpAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            // Generate 6-digit OTP code
            var otpCode = new Random().Next(100000, 999999).ToString();
            
            // Set OTP code and expiration (15 minutes)
            user.OtpCode = otpCode;
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(15);
            await _userManager.UpdateAsync(user);

            // Send email with OTP
            var subject = "Verify Your Email Address";
            var message = $"Your verification code is: {otpCode}. It will expire in 15 minutes.";
            await _emailService.SendEmailAsync(email, subject, message);

            return true;
        }

        // Helper methods
        private async Task<Tuple<string, string>> GenerateTokensAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName)
            };

            // Add roles
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            // Generate refresh token
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id
            };

            // Remove old refresh tokens
            var oldTokens = await _context.RefreshTokens
                .Where(r => r.UserId == user.Id && (r.IsRevoked || r.Expires < DateTime.UtcNow))
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(oldTokens);

            // Add new refresh token
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new Tuple<string, string>(jwtToken, refreshToken.Token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private string GenerateRandomPassword()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
