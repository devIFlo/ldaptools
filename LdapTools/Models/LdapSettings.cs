using Microsoft.AspNetCore.DataProtection;
using System.ComponentModel.DataAnnotations;

namespace LdapTools.Models
{
    public class LdapSettings
    {
        public int Id { get; set; }

        [Display(Name = "Dominio FQDN")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string FqdnDomain { get; set; }

        [Display(Name = "Porta")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required int Port { get; set; }

        [Display(Name = "Dominio NetBios")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string NetBiosDomain { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string BaseDn { get; set; }

        [Display(Name = "Usuário")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string UserDn { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string Password { get; set; }

        public void EncryptPassword(IDataProtector protector)
        {
            Password = protector.Protect(Password);
        }

        public string DecryptPassword(IDataProtector protector)
        {
            return protector.Unprotect(Password);
        }
    }
}
