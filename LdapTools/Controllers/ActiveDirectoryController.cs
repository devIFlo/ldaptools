using AspNetCoreHero.ToastNotification.Abstractions;
using LdapTools.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

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
        public async Task<IActionResult> UsersTable(string? ou, string? args, bool recursive = false, int? page = 1)
        {
            // Evita buscar sem OU e sem argumento
            if (string.IsNullOrWhiteSpace(ou) && string.IsNullOrWhiteSpace(args))
            {
                _notyfService.Warning("Informe um nome, matrícula ou selecione uma OU antes de buscar.");
                return PartialView("_UsersTable", new X.PagedList.PagedList<object>(new List<object>(), 1, 1));
            }

            const int pageSize = 10;
            var users = await _ldapExplorerService.GetUsersAsync(ou, args, recursive);
            var pagedUsers = users.ToPagedList(page ?? 1, pageSize);

            ViewBag.OU = ou;
            ViewBag.Args = args;

            return PartialView("_UsersTable", pagedUsers);
        }

        [HttpGet]
        public async Task<IActionResult> GetOuTree(string? parentDn = null)
        {
            var ous = await _ldapExplorerService.GetOuTreeAsync(parentDn);
            var nodes = ous.Select(ou => new
            {
                id = ou.DistinguishedName,
                text = ou.Name,
                children = true
            });

            return Json(nodes);
        }

        [HttpGet]
        public async Task<IActionResult> ImportUsers()
        {
            var ous = await _ldapExplorerService.GetAllOusAsync();
            ViewBag.Ous = ous;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportUsers(IFormFile file, string ou)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Selecione uma planilha válida.");
            }

            if (string.IsNullOrEmpty(ou))
            {
                ModelState.AddModelError("", "Selecione a OU de destino.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Ous = await _ldapExplorerService.GetAllOusAsync();
                return View();
            }

            var resultado = await _ldapExplorerService.ImportUsersAsync(file, ou);

            TempData["Mensagem"] = resultado;

            return RedirectToAction(nameof(ImportUsers));
        }
    }
}