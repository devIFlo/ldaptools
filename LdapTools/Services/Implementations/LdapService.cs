using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using LdapTools.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Serilog;
using System.Runtime.InteropServices;

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

        public static bool ValidateCertificate(byte[] rawCertData, string expectedServerName)
        {
            try
            {
                using X509Certificate2 cert = X509CertificateLoader.LoadCertificate(rawCertData);

                // Verifica a validade do certificado
                if (DateTime.UtcNow < cert.NotBefore || DateTime.UtcNow > cert.NotAfter)
                {
                    Console.WriteLine("Certificado expirado ou ainda não válido.");
                    return false;
                }

                // Permite variações no nome do CN
                if (!cert.Subject.Contains($"CN={expectedServerName}", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Nome do certificado inválido: {cert.Subject}");
                    return false;
                }

                // Valida a cadeia de certificados (confirma se a CA é confiável)
                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Evita erro se OCSP não estiver configurado
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                bool isValid = chain.Build(cert);
                if (!isValid)
                {
                    Console.WriteLine($"Falha na validação da cadeia: {chain.ChainStatus[0].StatusInformation}");
                    return false;
                }

                Console.WriteLine("Certificado válido!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao validar o certificado: {ex.Message}");
                return false;
            }
        }


        private async Task<LdapConnection> CreateLdapConnection()
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();

            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");

            var fqdnDomain = ldapSettings.FqdnDomain;
            var port = ldapSettings.Port;
            var netBiosDomain = ldapSettings.NetBiosDomain;
            var baseDn = ldapSettings.BaseDn;
            var userDn = ldapSettings.UserDn;
            var password = ldapSettings.DecryptPassword(_protector);

            var ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(fqdnDomain, port));
            ldapConnection.Credential = new NetworkCredential(userDn, password, netBiosDomain);
            ldapConnection.AuthType = AuthType.Negotiate;

            ldapConnection.SessionOptions.ProtocolVersion = 3;
            ldapConnection.SessionOptions.SecureSocketLayer = port == 636;

            ldapConnection.SessionOptions.VerifyServerCertificate += (conn, cert) =>
            {
                const string serverCertificateName = "floresta.ifsertao-pe.edu.br";
                return ValidateCertificate(cert.GetRawCertData(), serverCertificateName);
            };

            return ldapConnection;
        }

        public async Task<bool> IsAuthenticated(string username, string password)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();

            if (ldapSettings == null) return false;

            try
            {
                using (LdapConnection ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(ldapSettings.FqdnDomain, ldapSettings.Port)))
                {
                    ldapConnection.AuthType = AuthType.Basic;
                    ldapConnection.SessionOptions.ProtocolVersion = 3;
                    ldapConnection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
                    ldapConnection.Timeout = TimeSpan.FromMinutes(1);

                    ldapConnection.Bind(new NetworkCredential(username, password, ldapSettings.NetBiosDomain));

                    return true;
                }
            }
            catch (LdapException)
            {
                return false;
            }
        }

        public async Task<string?> GetEmailByUsernameAsync(string username)
        {
            using var ldapConnection = await CreateLdapConnection();
            ldapConnection.Bind();

            var searchRequest = new SearchRequest(
                "OU=USUARIOS,OU=IFSERTAOPE-CF,DC=ad,DC=floresta,DC=ifsertao-pe,DC=edu,DC=br",
                $"(sAMAccountName={username})",
                SearchScope.Subtree,
                "mail"
            );

            var response = (SearchResponse)ldapConnection.SendRequest(searchRequest);
            if (response.Entries.Count == 0) return null;

            var entry = response.Entries[0];
            return entry.Attributes["mail"]?[0]?.ToString();
        }

        public async Task<LdapUser> GetUserByEmailAsync(string email)
        {
            using var ldapConnection = await CreateLdapConnection();
            ldapConnection.Bind();

            var searchRequest = new SearchRequest(
                "OU=USUARIOS,OU=IFSERTAOPE-CF,DC=ad,DC=floresta,DC=ifsertao-pe,DC=edu,DC=br",
                $"(&(objectClass=user)(mail={email}))",
                SearchScope.Subtree,
                "distinguishedName"
            );

            var searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);

            if (searchResponse.Entries.Count > 0)
            {
                var entry = searchResponse.Entries[0];
                return new LdapUser
                {
                    DistinguishedName = entry.DistinguishedName
                };
            }

            return null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            using var ldapConnection = await CreateLdapConnection();

            try
            {             
                ldapConnection.Bind();

                if (!ldapConnection.SessionOptions.SecureSocketLayer)
                {
                    Console.WriteLine("Iniciando StartTLS...");
                    ldapConnection.SessionOptions.StartTransportLayerSecurity(null);
                }

                var modification = new DirectoryAttributeModification
                {
                    Name = "unicodePwd",
                    Operation = DirectoryAttributeOperation.Replace
                };

                modification.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));

                var modifyRequest = new ModifyRequest(user.DistinguishedName, modification);

                ldapConnection.SendRequest(modifyRequest);
                return true;
            }
            catch (DirectoryOperationException ex)
            {
                Console.WriteLine($"Erro ao alterar a senha: {ex.Message}");
                return false;
            }
        }

        public async Task<List<LdapUser>> GetLdapUsers()
        {
            var ldapUsers = new List<LdapUser>();

            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            if (ldapSettings == null) throw new InvalidOperationException("Configurações LDAP não encontradas.");

            using var ldapConnection = await CreateLdapConnection();

            try
            {
                ldapConnection.Bind();

                if (!ldapConnection.SessionOptions.SecureSocketLayer)
                {
                    Console.WriteLine("Iniciando StartTLS...");
                    ldapConnection.SessionOptions.StartTransportLayerSecurity(null);
                }
            }
            catch (LdapException ex)
            {
                throw new LdapException("Falha ao conectar ao servidor LDAP. Verifique as configurações da conexão.", ex);
            }

            var filter = "(objectClass=person)";
            var searchRequest = new SearchRequest(ldapSettings.BaseDn, filter, SearchScope.Subtree, null);
            var response = (SearchResponse)ldapConnection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                var ldapUser = new LdapUser
                {
                    Username = entry.Attributes["sAMAccountName"]?[0]?.ToString(),
                    Email = entry.Attributes["mail"]?[0]?.ToString(),
                    FirstName = entry.Attributes["givenName"]?[0]?.ToString(),
                    LastName = entry.Attributes["sn"]?[0]?.ToString()
                };

                ldapUsers.Add(ldapUser);
            }

            return ldapUsers;
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

            using var ldapConnection = await CreateLdapConnection();

            try
            {
                ldapConnection.Bind();
            }
            catch (LdapException ex)
            {
                throw new LdapException("Falha ao conectar ao servidor LDAP. Verifique as configurações da conexão.", ex);
            }

            var usernamesFilter = string.Join("", usernames.Select(username => $"(sAMAccountName={username})"));
            var filter = $"(&(objectClass=person)(|{usernamesFilter}))";

            var searchRequest = new SearchRequest(ldapSettings.BaseDn, filter, SearchScope.Subtree, null);
            var response = (SearchResponse)ldapConnection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                var ldapUser = new LdapUser
                {
                    Username = entry.Attributes["sAMAccountName"]?[0]?.ToString(),
                    Email = entry.Attributes["mail"]?[0]?.ToString(),
                    FirstName = entry.Attributes["givenName"]?[0]?.ToString(),
                    LastName = entry.Attributes["sn"]?[0]?.ToString()
                };

                ldapUsers.Add(ldapUser);
            }

            return ldapUsers;
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