namespace WalletNet.Models;

using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

public class User : IdentityUser
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEmailVerified { get; set; } = false;
    public string OtpCode { get; set; }
    public DateTime? OtpExpiration { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
}

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public virtual User User { get; set; }
}