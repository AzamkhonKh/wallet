using Microsoft.AspNetCore.DataProtection;

namespace WalletNet.DTOs;


public class UserDTO {
    public string Username { get; set;}
    public string Password { get; set;} 
    public string Email { get; set;}

}