using System.ComponentModel.DataAnnotations;

namespace LdapTools.ViewModels
{
    public class LdapUserViewModel
    {
        [Display(Name = "Nome")]
        public string? Name { get; set; }

        [Display(Name = "Usuário")]
        public string? Username { get; set; }

        [Display(Name = "E-mail")]
        public string? Email { get; set; }
        public string? DistinguishedName { get; set; }
    }
}
