using LdapTools.Data;
using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LdapTools.Repositories.Implementations
{
    public class LdapSettingsRepository : ILdapSettingsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IDataProtector _protector;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LdapSettingsRepository(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _dataProtectionProvider = dataProtectionProvider;
            _protector = _dataProtectionProvider.CreateProtector("LdapSettingsPasswordProtector");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LdapSettings?> GetLdapSettings()
        {
            return await _context.LdapSettings.FirstOrDefaultAsync();
        }

        public async Task<LdapSettings> Add(LdapSettings ldapSettings)
        {
            ldapSettings.EncryptPassword(_protector);

            _context.LdapSettings.Add(ldapSettings);
            await _context.SaveChangesAsync();

            var currentUser = _httpContextAccessor.HttpContext?.User.Identity?.Name;

            Log.Information("O usuário {CurrentUser} adicionou as configurações do LDAP em {Timestamp}",
                    currentUser, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            return ldapSettings;
        }

        public async Task<LdapSettings> Update(LdapSettings ldapSettings)
        {
            var ldapSettingsDB = await GetLdapSettings();

            if (ldapSettingsDB == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");

            ldapSettingsDB.FqdnDomain = ldapSettings.FqdnDomain;
            ldapSettingsDB.Port = ldapSettings.Port;
            ldapSettingsDB.BaseDn = ldapSettings.BaseDn;
            ldapSettingsDB.NetBiosDomain = ldapSettings.NetBiosDomain;
            ldapSettingsDB.UserDn = ldapSettings.UserDn;
            ldapSettingsDB.Password = ldapSettings.Password;

            ldapSettingsDB.EncryptPassword(_protector);

            _context.LdapSettings.Update(ldapSettingsDB);
            await _context.SaveChangesAsync();

            var currentUser = _httpContextAccessor.HttpContext?.User.Identity?.Name;

            Log.Information("O usuário {CurrentUser} alterou as configurações do LDAP em {Timestamp}",
                    currentUser, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            return ldapSettingsDB;
        }
    }
}