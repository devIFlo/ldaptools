using LdapTools.Data;
using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LdapTools.Repositories.Implementations
{
    public class EmailSettingsRepository : IEmailSettingsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IDataProtector _protector;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailSettingsRepository(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _dataProtectionProvider = dataProtectionProvider;
            _protector = _dataProtectionProvider.CreateProtector("EmailSettingsPasswordProtector");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<EmailSettings?> GetEmailSettings()
        {
            return await _context.EmailSettings.FirstOrDefaultAsync();
        }

        public async Task<EmailSettings> Add(EmailSettings emailSettings)
        {
            emailSettings.EncryptPassword(_protector);

            _context.EmailSettings.Add(emailSettings);
            await _context.SaveChangesAsync();

            var currentUser = _httpContextAccessor.HttpContext?.User.Identity?.Name;

            Log.Information("O usuário {CurrentUser} adicionou as configurações do servidor de e-mail em {Timestamp}",
                    currentUser, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            return emailSettings;
        }

        public async Task<EmailSettings> Update(EmailSettings emailSettings)
        {
            var emailSettingsDB = await GetEmailSettings();

            if (emailSettingsDB == null) throw new InvalidOperationException("Configurações de E-mail não encontradas.");

            emailSettingsDB.SmtpServer = emailSettings.SmtpServer;
            emailSettingsDB.SmtpPort = emailSettings.SmtpPort;
            emailSettingsDB.SenderName = emailSettings.SenderName;
            emailSettingsDB.SenderEmail = emailSettings.SenderEmail;
            emailSettingsDB.Username = emailSettings.Username;
            emailSettingsDB.Password = emailSettings.Password;
            emailSettingsDB.EnableSsl = emailSettings.EnableSsl;

            emailSettingsDB.EncryptPassword(_protector);

            _context.EmailSettings.Update(emailSettingsDB);
            await _context.SaveChangesAsync();

            var currentUser = _httpContextAccessor.HttpContext?.User.Identity?.Name;

            Log.Information("O usuário {CurrentUser} alterou as configurações do servidor de e-mail em {Timestamp}",
                    currentUser, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            return emailSettingsDB;
        }
    }
}