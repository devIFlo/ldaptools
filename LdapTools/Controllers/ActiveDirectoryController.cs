using AspNetCoreHero.ToastNotification.Abstractions;
using LdapTools.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LdapTools.ViewModels;

namespace LdapTools.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActiveDirectoryController : Controller
    {
        private readonly INotyfService _notyfService;
        private readonly ILdapExplorerService _ldapExplorerService;

        public ActiveDirectoryController(INotyfService notyfService, ILdapExplorerService ldapExplorerService)
        {
            _notyfService = notyfService;
            _ldapExplorerService = ldapExplorerService;
        }

        [HttpGet]
        public IActionResult Users()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(string? ou, string? args, bool recursive = false)
        {
            var users = await _ldapExplorerService.GetUsersAsync(ou, args, recursive);
            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetOuTree(string? parentDn = null)
        {
            var ous = await _ldapExplorerService.GetOuTreeAsync(parentDn);
            var nodes = ous.Select(ou => new
            {
                id = ou.DistinguishedName,
                text = ou.Name,
                children = true // diz ao jsTree que pode expandir
            });

            return Json(nodes);
        }
    }
}
