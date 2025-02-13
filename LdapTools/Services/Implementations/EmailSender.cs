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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailSender(IEmailSettingsRepository emailSettingsRepository,
            IDataProtectionProvider dataProtectionProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _emailSettingsRepository = emailSettingsRepository;
            _protector = dataProtectionProvider.CreateProtector("EmailSettingsPasswordProtector");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendPasswordResetEmailAsync(string email, string token)
        {
            var emailSettings = await _emailSettingsRepository.GetEmailSettings();

            if (emailSettings == null) throw new InvalidOperationException("Configurações de Email não encontradas.");

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                throw new InvalidOperationException("A requisição HTTP não está disponível.");
            }

            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var callbackUrl = $"{baseUrl}/Account/ResetPassword?token={token}";

            var message = new MailMessage
            {
                From = new MailAddress(emailSettings.SenderEmail, emailSettings.SenderName),
                Subject = "Recuperação de Senha",
                Body = $@"
                    <p>Olá,</p>

                    <p>Você solicitou a redefinição de sua senha. Para prosseguir, clique no link abaixo:</p>

                    <p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='color: #007bff; text-decoration: none; font-weight: bold;'>Redefinir minha senha</a></p>

                    <p>Se você não solicitou essa alteração, ignore este e-mail. Sua senha permanecerá a mesma.</p>

                    <p>Atenciosamente,</p>
                    <p><strong>Coordenação de Tecnologia da Informação</strong><br />
                    Campus Floresta</strong></p>
                    ",
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