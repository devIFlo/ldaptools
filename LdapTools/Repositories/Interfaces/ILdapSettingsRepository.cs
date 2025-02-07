using LdapTools.Models;

namespace LdapTools.Repositories.Interfaces
{
    public interface ILdapSettingsRepository
    {
        Task<LdapSettings?> GetLdapSettings();
        Task<LdapSettings> Add(LdapSettings ldapSettings);
        Task<LdapSettings> Update(LdapSettings ldapSettings);
    }
}
