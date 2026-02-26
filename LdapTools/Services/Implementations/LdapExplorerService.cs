using ExcelDataReader;
using LdapTools.Repositories.Interfaces;
using LdapTools.Services.Interfaces;
using LdapTools.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;
using System.Text;

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

        public async Task<List<string>> GetAllOusAsync()
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
                new[] { "distinguishedName" },
                false
            );

            while (search.HasMore())
            {
                try
                {
                    var entry = search.Next();
                    var dn = entry.GetAttribute("distinguishedName")?.StringValue;
                    if (!string.IsNullOrEmpty(dn))
                        lista.Add(dn);
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

        public async Task<string> ImportUsersAsync(IFormFile file, string ou)
        {
            int criados = 0;
            int ignorados = 0;
            int erros = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet();
            var table = result.Tables[0];

            for (int i = 1; i < table.Rows.Count; i++) // pula cabeçalho
            {
                try
                {
                    string matricula = table.Rows[i][0]?.ToString()?.Trim();
                    string nome = table.Rows[i][1]?.ToString()?.Trim();
                    string email = table.Rows[i][2]?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(matricula) ||
                        string.IsNullOrWhiteSpace(nome))
                    {
                        ignorados++;
                        continue;
                    }

                    if (!await UserExists(matricula))
                    {
                        await CreateUserInAd(matricula, nome, email, ou);
                        criados++;
                    }
                    else
                    {
                        ignorados++;
                    }
                }
                catch
                {
                    erros++;
                }
            }

            return $"Importação finalizada: {criados} criados, {ignorados} ignorados, {erros} erros.";
        }

        public async Task<bool> UserExists(string username)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            var password = ldapSettings.DecryptPassword(_protector);

            using var connection = new LdapConnection();
            connection.Connect(ldapSettings.FqdnDomain, ldapSettings.Port);
            connection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            var filter = $"(&(objectClass=user)(sAMAccountName={username}))";

            var search = connection.Search(
                ldapSettings.BaseDn,
                LdapConnection.ScopeSub,
                filter,
                new[] { "sAMAccountName" },
                false
            );

            bool exists = search.HasMore();

            connection.Disconnect();

            return exists;
        }

        private async Task CreateUserInAd(string matricula, string nome, string email, string ouDn)
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();
            var password = ldapSettings.DecryptPassword(_protector);

            using var connection = new LdapConnection();

            connection.SecureSocketLayer = true;

            connection.Connect(ldapSettings.FqdnDomain, 636);
            connection.Bind($"{ldapSettings.UserDn}@{ldapSettings.FqdnDomain}", password);

            string userDn = $"CN={nome},{ouDn}";

            var attributes = new LdapAttributeSet
            {
                new LdapAttribute("objectClass", new string[] { "top", "person", "organizationalPerson", "user" }),
                new LdapAttribute("cn", nome),
                new LdapAttribute("sAMAccountName", matricula),
                new LdapAttribute("userPrincipalName", $"{matricula}@{ldapSettings.FqdnDomain}"),
                new LdapAttribute("displayName", nome),
                new LdapAttribute("mail", email ?? ""),
                new LdapAttribute("userAccountControl", "544") // Criado desabilitado
            };

            var newUser = new LdapEntry(userDn, attributes);

            connection.Add(newUser);

            // DEFINIR SENHA
            string senha = "Senha2023"; // você pode gerar dinâmica
            var senhaBytes = Encoding.Unicode.GetBytes($"\"{senha}\"");

            var modSenha = new LdapModification(
                LdapModification.Replace,
                new LdapAttribute("unicodePwd", senhaBytes)
            );

            connection.Modify(userDn, modSenha);

            // HABILITAR USUÁRIO (NORMAL_ACCOUNT = 512)
            var modEnable = new LdapModification(
                LdapModification.Replace,
                new LdapAttribute("userAccountControl", "512")
            );

            connection.Modify(userDn, modEnable);

            // ALTERAR A SENHA NO PRIMEIRO LOGON
            var modForceChange = new LdapModification(
                LdapModification.Replace,
                new LdapAttribute("pwdLastSet", "0")
            );

            connection.Modify(userDn, modForceChange);

            connection.Disconnect();
        }
    }
}
