using System.ComponentModel.DataAnnotations;

namespace LdapTools.VewModels
{
	public class ForgotPasswordViewModel
    {
		public string? FortigateToken { get; set; }
        public string? Username { get; set; }
    }
}
