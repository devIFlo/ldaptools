using AspNetCoreHero.ToastNotification.Abstractions;
using LdapTools.Models;
using LdapTools.Services.Interfaces;
using LdapTools.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LdapTools.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly INotyfService _notyfService;
        private readonly ILdapService _ldapService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            INotyfService notyfService,
            ILdapService ldapService)
        {            
            _userManager = userManager;
            _roleManager = roleManager;
            _notyfService = notyfService;
            _ldapService = ldapService;
        }

        public IActionResult Index()
        {
            var users = _userManager.Users;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return Json(new { message = "Usuário não encontrado!" });
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var roles = await _roleManager.Roles.ToListAsync();

            var selectedRole = roles.FirstOrDefault(r => r.Name != null && userRoles.Contains(r.Name))?.Name;

            var usersEditViewModel = new UsersEditViewModel
            {
                UserId = id,
                UserName = user.UserName,
                Roles = roles,
                SelectedRole = selectedRole
            };

            return View(usersEditViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsersEditViewModel model)
        {
            var selectedRole = model.SelectedRole;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user != null && selectedRole != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);

                    if (userRoles.Any())
                    {
                        var removeRoleResult = await _userManager.RemoveFromRolesAsync(user, userRoles);
                        if (!removeRoleResult.Succeeded)
                        {
                            _notyfService.Error("Erro ao remover os grupos existentes.");
                            return View(model);
                        }
                    }

                    var addRoleResult = await _userManager.AddToRoleAsync(user, selectedRole);
                    if (!addRoleResult.Succeeded)
                    {
                        _notyfService.Error("Erro ao adicionar o novo grupo.");
                        return View(model);
                    }

                    _notyfService.Success("Dados alterados com sucesso!");

                    var currentUser = HttpContext?.User.Identity?.Name;

                    Log.Information("O usuário {CurrentUser} alterou o grupo do usuário {User} em {Timestamp}",
                        currentUser, user.UserName, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    return RedirectToAction("Index");
                }
            }

            _notyfService.Error("Usuário não encontrado.");

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { message = "Usuário não encontrado!" });
            }

            return PartialView("_Delete", user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                _notyfService.Error($"Usuário com Id = {id} não foi encontrado");
            }
            else
            {
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _notyfService.Success($"Usuário {user.UserName} excluído com sucesso.");

                    var currentUser = HttpContext?.User.Identity?.Name;

                    Log.Information("O usuário {CurrentUser} deletou o usuário {UserName} (ID: {UserId}) em {Timestamp}",
                        currentUser, user.UserName, user.Id, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                }

                foreach (var error in result.Errors)
                {
                    _notyfService.Error(error.Description);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ImportLdapUsers(int id)
        {
            var ldapUsers = await _ldapService.GetLdapUsers();
            if (ldapUsers == null)
            {
                return Json(new { message = "Usuários não encontrados!" });
            }

            var importLdapUsersViewModel = new ImportLdapUsersViewModel
            {
                LdapUsers = ldapUsers                
            };

            return PartialView("_ImportLdapUsers", importLdapUsersViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportLdapUsers(ImportLdapUsersViewModel importLdapUsersViewModel)
        {
            var usernames = importLdapUsersViewModel.SelectedUsernames;

            try
            {
                if (usernames != null)
                {
                    var ldapUsers = await _ldapService.GetLdapUsers(usernames);
                    await _ldapService.ImportLdapUsers(ldapUsers);
                    _notyfService.Success("Usuários importados com sucesso.");
                }
            }
            catch (Exception ex)
            {
                _notyfService.Error("Não foi possivel importar os usuários: " + ex.Message);
            }

            return RedirectToAction("Index");
        }
    }
}
