using Microsoft.AspNetCore.WebUtilities;
using System.Net.Mail;
using System.Net;
using System.Text.Encodings.Web;
using System.Text;
using LdapTools.Services.Interfaces;
using LdapTools.Repositories.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace LdapTools.Services.Implementations
{
    public class EmailSender : IEmailSender
    {
        private readonly IEmailSettingsRepository _emailSettingsRepository;
        private readonly IDataProtector _protector;

        public EmailSender(IEmailSettingsRepository emailSettingsRepository, IDataProtectionProvider dataProtectionProvider)
        {
            _emailSettingsRepository = emailSettingsRepository;
            _protector = dataProtectionProvider.CreateProtector("EmailSettingsPasswordProtector");
        }

        public async Task SendPasswordResetEmailAsync(string email, string token)
        {
            var emailSettings = await _emailSettingsRepository.GetEmailSettings();

            if (emailSettings == null) throw new InvalidOperationException("Configurações de Email não encontradas.");

            var callbackUrl = $"https://localhost:7248/Account/ResetPassword?token={token}";

            var message = new MailMessage
            {
                From = new MailAddress(emailSettings.SenderEmail, emailSettings.SenderName),
                Subject = "Recuperação de Senha",
                Body = $"Clique <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>aqui</a> para redefinir sua senha.",
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(email));

            var password = emailSettings.DecryptPassword(_protector);

            using var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(emailSettings.Username, password),
                EnableSsl = emailSettings.EnableSsl
            };

            await client.SendMailAsync(message);
        }
    }
}