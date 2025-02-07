using Microsoft.AspNetCore.DataProtection;
using System.ComponentModel.DataAnnotations;

namespace LdapTools.Models
{
    public class EmailSettings
    {
        public int Id { get; set; }

        [Display(Name = "Servidor")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string SmtpServer { get; set; }

        [Display(Name = "Porta")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public int SmtpPort { get; set; }

        [Display(Name = "Nome do remetente")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string SenderName { get; set; }

        [Display(Name = "E-mail do remetente")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string SenderEmail { get; set; }

        [Display(Name = "Usuário")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string Username { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        public required string Password { get; set; }

        [Display(Name = "Ativar SSL")]
        public bool EnableSsl { get; set; }

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
