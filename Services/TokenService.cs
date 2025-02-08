namespace WalletNet.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WalletNet.Models;

public class TokenService
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _dbContext;

    public TokenService(IConfiguration config, ApplicationDbContext dbContext)
    {
        _config = config;
        _dbContext = dbContext;
    }

    public async Task<string> GenerateTokenAsync(User user, string clientIp, string deviceInfo)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationMinutes"]));
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        // Save token in the database
        var tokenEntity = new Token
        {
            TokenValue = tokenValue,
            ExpiryDate = expiration,
            ClientIP = clientIp,
            DeviceInfo = deviceInfo,
            UserId = user.Id
        };

        _dbContext.Tokens.Add(tokenEntity);
        await _dbContext.SaveChangesAsync();

        return tokenValue;
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        var storedToken = _dbContext.Tokens.FirstOrDefault(t => t.TokenValue == token);
        if (storedToken == null) return false;

        storedToken.IsRevoked = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
