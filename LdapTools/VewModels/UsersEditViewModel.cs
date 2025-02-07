using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LdapTools.VewModels
{
    public class UsersEditViewModel
    {
        [Required]
        public required string UserId { get; set; }

        public string? UserName { get; set; }

        [Display(Name = "Grupo")]
        [Required]
        public string? SelectedRole { get; set; }

        public IEnumerable<IdentityRole>? Roles { get; set; }
    }
}
