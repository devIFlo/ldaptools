using LdapTools.Models;

namespace LdapTools.ViewModels
{
    public class ImportLdapUsersViewModel
    {
        public List<LdapUser>? LdapUsers { get; set; }
        public List<string>? SelectedUsernames { get; set; }
    }
}
