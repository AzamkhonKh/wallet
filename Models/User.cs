namespace WalletNet.Models;
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; } // Store hashed passwords
    public ICollection<Token> Tokens { get; set; } = new List<Token>();
}
