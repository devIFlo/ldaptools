using LdapTools.Repositories.Interfaces;
using LdapTools.Services.Interfaces;
using LdapTools.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Novell.Directory.Ldap;

namespace LdapTools.Services.Implementations
{
    public class LdapExplorerService : ILdapExplorerService
    {
        private readonly ILdapSettingsRepository _ldapSettingsRepository;
        private readonly IDataProtector _protector;

        public LdapExplorerService(ILdapSettingsRepository ldapSettingsRepository, IDataProtectionProvider dataProtectionProvider)
        {
            _ldapSettingsRepository = ldapSettingsRepository;
            _protector = dataProtectionProvider.CreateProtector("LdapSettingsPasswordProtector");
        }

        public async Task<List<string>> GetAllOUs()
        {
            var lista = new List<string>();

            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            var password = ldapSettings.DecryptPassword(_protector);

            using var connection = new LdapConnection();
            connection.Connect(ldapSettings.FqdnDomain, ldapSettings.Port);
            connection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            var search = connection.Search(
                ldapSettings.BaseDn,
                LdapConnection.ScopeSub,
                "(objectClass=organizationalUnit)",
                new[] { "ou" },
                false
            );

            while (search.HasMore())
            {
                try
                {
                    var entry = search.Next();
                    var ou = entry.GetAttribute("ou")?.StringValue;
                    if (!string.IsNullOrEmpty(ou))
                        lista.Add(ou);
                }
                catch (LdapException) { continue; }
            }

            connection.Disconnect();
            return lista.OrderBy(o => o).ToList();
        }

        public async Task<List<LdapUserViewModel>> GetUsersAsync(string? ou = null, string? args = null, bool recursive = false)
        {
            var resultado = new List<LdapUserViewModel>();

            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            var password = ldapSettings.DecryptPassword(_protector);

            using var connection = new LdapConnection();
            connection.Connect(ldapSettings.FqdnDomain, ldapSettings.Port);
            connection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            string searchBase = string.IsNullOrWhiteSpace(ou) ? ldapSettings.BaseDn : ou;

            string filtro = "(objectClass=user)";
            if (!string.IsNullOrWhiteSpace(args))
                filtro = $"(&(objectClass=user)(|(cn=*{args}*)(sAMAccountName=*{args}*)))";

            var search = connection.Search(
                searchBase,
                recursive ? LdapConnection.ScopeSub : LdapConnection.ScopeOne,
                filtro,
                new[] { "cn", "sAMAccountName", "mail", "distinguishedName" },
                false
            );

            while (search.HasMore())
            {
                try
                {
                    var entry = search.Next();
                    var attrs = entry.GetAttributeSet();

                    resultado.Add(new LdapUserViewModel
                    {
                        Name = attrs.GetAttribute("cn")?.StringValue,
                        Username = attrs.GetAttribute("sAMAccountName")?.StringValue,
                        Email = attrs.ContainsKey("mail") ? entry.GetAttribute("mail")?.StringValue : string.Empty,
                        DistinguishedName = entry.Dn
                    });
                }
                catch (LdapException) { continue; }
            }

            connection.Disconnect();
            return resultado.OrderBy(u => u.Name).ToList();
        }


        public async Task<List<OrganizationalUnitViewModel>> GetOuTreeAsync(string? parentDn = null)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            var password = ldapSettings.DecryptPassword(_protector);

            using var connection = new LdapConnection();
            connection.Connect(ldapSettings.FqdnDomain, ldapSettings.Port);
            connection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            string searchBase = string.IsNullOrEmpty(parentDn) ? ldapSettings.BaseDn : parentDn;

            var search = connection.Search(
                searchBase,
                LdapConnection.ScopeOne,
                "(|(objectClass=organizationalUnit)(objectClass=container))",
                new[] { "ou", "name", "distinguishedName", "objectClass" },
                false
            );

            var ous = new List<OrganizationalUnitViewModel>();

            while (search.HasMore())
            {
                try
                {
                    var entry = search.Next();
                    var ou = entry.GetAttribute("ou")?.StringValue ?? entry.GetAttribute("name")?.StringValue;

                    var dn = entry.Dn;

                    if (!string.IsNullOrEmpty(ou))
                    {
                        ous.Add(new OrganizationalUnitViewModel
                        {
                            Name = ou,
                            DistinguishedName = dn
                        });
                    }
                }
                catch (LdapException) { continue; }
            }

            connection.Disconnect();
            return ous.OrderBy(o => o.Name).ToList();
        }
    }
}
