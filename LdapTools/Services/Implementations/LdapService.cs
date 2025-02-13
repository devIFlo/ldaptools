using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using LdapTools.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Novell.Directory.Ldap;
using Serilog;

namespace LdapTools.Services.Implementations
{
    public class LdapService : ILdapService
    {
        private readonly ILdapSettingsRepository _ldapSettingsRepository;
        private readonly IDataProtector _protector;
        private readonly UserManager<ApplicationUser> _userManager;

        public LdapService(
            ILdapSettingsRepository ldapSettingsRepository,
            IDataProtectionProvider dataProtectionProvider,
            UserManager<ApplicationUser> userManager)
        {
            _ldapSettingsRepository = ldapSettingsRepository;
            _protector = dataProtectionProvider.CreateProtector("LdapSettingsPasswordProtector");
            _userManager = userManager;
        }

        private async Task<LdapConnection> CreateLdapConnectionAsync()
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");

            var ldapConnection = new LdapConnection();
            ldapConnection.Connect(ldapSettings.FqdnDomain, ldapSettings.Port);
            ldapConnection.SecureSocketLayer = ldapSettings.Port == 636;         

            return ldapConnection;
        }

        public async Task<bool> IsAuthenticated(string username, string password)
        {
            try
            {
                var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
                if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");

                var ldapConnection = new LdapConnection();
                ldapConnection.Connect(ldapSettings.FqdnDomain, ldapSettings.Port);
                ldapConnection.SecureSocketLayer = ldapSettings.Port == 636;

                ldapConnection.Bind($"{username}@{ldapSettings.FqdnDomain}", password);
                return ldapConnection.Bound;
            }
            catch (LdapException)
            {
                return false;
            }
        }

        public async Task<string?> GetEmailByUsernameAsync(string username)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");
            var password = ldapSettings.DecryptPassword(_protector);

            using var ldapConnection = await CreateLdapConnectionAsync();
            ldapConnection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            var searchFilter = $"(sAMAccountName={username})";
            var searchResults = ldapConnection.Search(ldapSettings.BaseDn, LdapConnection.ScopeSub, searchFilter, new[] { "mail" }, false);

            if (searchResults.HasMore())
            {
                var entry = searchResults.Next();
                return entry.GetAttributeSet().ContainsKey("mail") ? entry.GetAttribute("mail")?.StringValue : string.Empty;
            }

            return null;
        }

        public async Task<LdapUser?> GetUserByEmailAsync(string email)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");
            var password = ldapSettings.DecryptPassword(_protector);

            using var ldapConnection = await CreateLdapConnectionAsync();
            ldapConnection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            var searchFilter = $"(mail={email})";
            var searchResults = ldapConnection.Search(ldapSettings.BaseDn, LdapConnection.ScopeSub, searchFilter, new[] { "sAMAccountName", "givenName", "sn", "distinguishedName" }, false);

            if (searchResults.HasMore())
            {
                var entry = searchResults.Next();
                return new LdapUser
                {
                    Username = entry.GetAttribute("sAMAccountName")?.StringValue,
                    FirstName = entry.GetAttributeSet().ContainsKey("givenName") ? entry.GetAttribute("givenName")?.StringValue : string.Empty,
                    LastName = entry.GetAttributeSet().ContainsKey("sn") ? entry.GetAttribute("sn")?.StringValue : string.Empty,
                    DistinguishedName = entry.GetAttribute("distinguishedName")?.StringValue,
                };
            }
            return null;
        }

        public async Task<List<LdapUser>> GetLdapUsers()
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");
            var password = ldapSettings.DecryptPassword(_protector);

            using var ldapConnection = await CreateLdapConnectionAsync();
            ldapConnection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            var searchResults = ldapConnection.Search(ldapSettings.BaseDn, LdapConnection.ScopeSub, "(objectClass=user)", new[] { "sAMAccountName", "mail", "givenName", "sn" }, false);

            var users = new List<LdapUser>();
            while (searchResults.HasMore())
            {
                var entry = searchResults.Next();

                var attributeSet = entry.GetAttributeSet();
                foreach (LdapAttribute attr in attributeSet)
                {
                    Console.WriteLine($"{attr.Name}: {attr.StringValue}");
                }

                users.Add(new LdapUser
                {
                    Username = entry.GetAttribute("sAMAccountName")?.StringValue,
                    Email = entry.GetAttributeSet().ContainsKey("mail") ? entry.GetAttribute("mail")?.StringValue : string.Empty,
                    FirstName = entry.GetAttributeSet().ContainsKey("givenName") ? entry.GetAttribute("givenName")?.StringValue : string.Empty,
                    LastName = entry.GetAttributeSet().ContainsKey("sn") ? entry.GetAttribute("sn")?.StringValue : string.Empty
                });
            }

            return users;
        }

        public async Task<List<LdapUser>> GetLdapUsers(List<string> usernames)
        {
            var ldapUsers = new List<LdapUser>();
            if (usernames == null || !usernames.Any())
            {
                return ldapUsers;
            }

            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");
            var password = ldapSettings.DecryptPassword(_protector);

            using var ldapConnection = await CreateLdapConnectionAsync();
            ldapConnection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            var usernameFilters = string.Join("", usernames.Select(username => $"(sAMAccountName={username})"));
            var searchFilter = $"(&(objectClass=person)(|{usernameFilters}))";

            var searchResults = ldapConnection.Search(
                ldapSettings.BaseDn, 
                LdapConnection.ScopeSub,
                searchFilter, 
                new[] { "sAMAccountName", "mail", "givenName", "sn" }, 
                false
            );

            while (searchResults.HasMore())
            {
                var entry = searchResults.Next();

                ldapUsers.Add(new LdapUser
                {
                    Username = entry.GetAttribute("sAMAccountName")?.StringValue,
                    Email = entry.GetAttributeSet().ContainsKey("mail") ? entry.GetAttribute("mail")?.StringValue : string.Empty,
                    FirstName = entry.GetAttributeSet().ContainsKey("givenName") ? entry.GetAttribute("givenName")?.StringValue : string.Empty,
                    LastName = entry.GetAttributeSet().ContainsKey("sn") ? entry.GetAttribute("sn")?.StringValue : string.Empty
                });
            }

            return ldapUsers;
        }
               
        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");
            var password = ldapSettings.DecryptPassword(_protector);

            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            using var ldapConnection = await CreateLdapConnectionAsync();
            ldapConnection.StartTls();
            ldapConnection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            try
            {
                var passwordBytes = Encoding.Unicode.GetBytes($"\"{newPassword}\"");
                var modification = new LdapModification(
                    LdapModification.Replace, 
                    new LdapAttribute("unicodePwd", passwordBytes)
                );

                ldapConnection.Modify(user.DistinguishedName, new LdapModification[] { modification });

                return true;
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"Erro ao alterar a senha: {ex.Message}");
                return false;
            }
        }

        public async Task ImportLdapUsers(List<LdapUser> ldapUsers)
        {
            var identityUsers = _userManager.Users.ToList();

            foreach (var ldapUser in ldapUsers)
            {
                var identityUser = identityUsers.FirstOrDefault(u => u.UserName == ldapUser.Username);

                if (identityUser == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = ldapUser.Username,
                        Email = ldapUser.Email,
                        FirstName = ldapUser.FirstName,
                        LastName = ldapUser.LastName,
                        UserType = "LDAP"
                    };

                    var result = await _userManager.CreateAsync(newUser);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, "VIEWER");
                    }
                    else
                    {
                        Log.Error("{Timestamp} - Falha ao criar o usuário {UserName}: {Errors}",
                            DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ldapUser.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
    }
}