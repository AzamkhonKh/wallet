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
    public virtual ICollection<BudgetMaster.Models.Space> Spaces { get; set; } // Added using fully qualified name
    public virtual ICollection<Transaction> Transactions { get; set; } // Added Transactions collection

    public User() // Added constructor if not present, or added to existing
    {
        RefreshTokens = new HashSet<RefreshToken>(); // Assuming this was initialized, if not, it should be
        Spaces = new HashSet<BudgetMaster.Models.Space>();
        Transactions = new HashSet<Transaction>(); // Initialized Transactions collection
    }
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