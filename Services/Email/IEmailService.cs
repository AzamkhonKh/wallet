using System.Threading.Tasks;

namespace WalletNet.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}
