using LdapTools.Models;

namespace LdapTools.Repositories.Interfaces
{
    public interface IEmailSettingsRepository
    {
        Task<EmailSettings?> GetEmailSettings();
        Task<EmailSettings> Add(EmailSettings emailSettings);
        Task<EmailSettings> Update(EmailSettings emailSettings);
    }
}