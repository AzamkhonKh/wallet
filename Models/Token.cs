namespace WalletNet.Models;

public class Token
{
    public int Id { get; set; }
    public string TokenValue { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string ClientIP { get; set; }
    public string DeviceInfo { get; set; }
    public bool IsRevoked { get; set; } = false;

    public int UserId { get; set; }
    public User User { get; set; }
}
