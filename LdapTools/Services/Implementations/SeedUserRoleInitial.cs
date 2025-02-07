using LdapTools.Models;
using LdapTools.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LdapTools.Services.Implementations
{
    public class SeedUserRoleInitial : ISeedUserRoleInitial
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SeedUserRoleInitial(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedRolesAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                IdentityRole role = new IdentityRole();
                role.Name = "Admin";
                role.NormalizedName = "ADMIN";
                role.ConcurrencyStamp = Guid.NewGuid().ToString();

                IdentityResult roleResult = await _roleManager.CreateAsync(role);
            }

            if (!await _roleManager.RoleExistsAsync("Editor"))
            {
                IdentityRole role = new IdentityRole();
                role.Name = "Editor";
                role.NormalizedName = "EDITOR";
                role.ConcurrencyStamp = Guid.NewGuid().ToString();

                IdentityResult roleResult = await _roleManager.CreateAsync(role);
            }

            if (!await _roleManager.RoleExistsAsync("Viewer"))
            {
                IdentityRole role = new IdentityRole();
                role.Name = "Viewer";
                role.NormalizedName = "VIEWER";
                role.ConcurrencyStamp = Guid.NewGuid().ToString();

                IdentityResult roleResult = await _roleManager.CreateAsync(role);
            }
        }

        public async Task SeedUsersAsync()
        {
            if (await _userManager.FindByNameAsync("admin") == null)
            {
                ApplicationUser user = new ApplicationUser();
                user.UserName = "admin";
                user.NormalizedUserName = "ADMIN";
                user.FirstName = "Administrador";
                user.Email = "admin@admin.com";
                user.UserType = "LOCAL";
                user.LockoutEnabled = false;
                user.SecurityStamp = Guid.NewGuid().ToString();

                IdentityResult result = await _userManager.CreateAsync(user, "Admin@2025");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}