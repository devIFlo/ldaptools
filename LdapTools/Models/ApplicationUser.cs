using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LdapTools.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Usuário")]
        public override string? UserName { get; set; }

        [Display(Name = "Nome")]
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Display(Name = "Tipo")]
        public string? UserType { get; set; }

        [NotMapped]
        [Display(Name = "Nome")]
        public string? FullName => $"{FirstName} {LastName}";
    }
}
