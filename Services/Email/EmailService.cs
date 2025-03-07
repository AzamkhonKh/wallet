using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WalletNet.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"]);
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"];

                var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
