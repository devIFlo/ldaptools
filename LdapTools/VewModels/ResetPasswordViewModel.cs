using System.ComponentModel.DataAnnotations;

namespace LdapTools.VewModels
{
    public class ResetPasswordViewModel
    {
        [Display(Name = "Nome")]
        public string? Name { get; set; }

        public string Email { get; set; }

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(50, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 7)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação não coincidem.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
