using System.ComponentModel.DataAnnotations;

namespace LdapTools.ViewModels
{
	public class ForgotPasswordViewModel
    {
		public string? FortigateLogin { get; set; }
        public string? Username { get; set; }
    }
}
