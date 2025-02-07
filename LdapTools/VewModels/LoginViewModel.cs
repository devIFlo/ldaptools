using System.ComponentModel.DataAnnotations;

namespace LdapTools.VewModels
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "O usuário é obrigatório")]
		[Display(Name = "Usuário")]
		public string? Username { get; set; }

		[Required(ErrorMessage = "A senha é obrigatória")]
		[DataType(DataType.Password)]
		[Display(Name = "Senha")]
		public string? Password { get; set; }

		[Display(Name = "Lembrar-me")]
		public bool RememberMe { get; set; }
	}
}
