namespace WalletNet.Models;

public class Token
{
    public int Id { get; set; }
    public required string TokenValue { get; set; }
    public DateTime ExpiryDate { get; set; }
    public required string ClientIP { get; set; }
    public required string DeviceInfo { get; set; }
    public bool IsRevoked { get; set; } = false;

    public int UserId { get; set; }
    public User? User { get; set; }
}
