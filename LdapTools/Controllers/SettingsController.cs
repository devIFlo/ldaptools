using AspNetCoreHero.ToastNotification.Abstractions;
using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LdapTools.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ILdapSettingsRepository _ldapSettingsRepository;
        private readonly IEmailSettingsRepository _emailSettingsRepository;
        private readonly INotyfService _notyfService;

        public SettingsController(
            ILdapSettingsRepository ldapSettingsRepository,
            IEmailSettingsRepository emailSettingsRepository,
            INotyfService notyfService)
        {
            _ldapSettingsRepository = ldapSettingsRepository;
            _emailSettingsRepository = emailSettingsRepository;
            _notyfService = notyfService;
        }

        public async Task<IActionResult> Ldap()
        {
            var ldapSettings = await _ldapSettingsRepository.GetLdapSettings();

            if (ldapSettings == null)
            {
                return View();
            }

            return View(ldapSettings);
        }

        [HttpPost]
        public async Task<IActionResult> Ldap(LdapSettings ldapSettings)
        {
            if (ModelState.IsValid)
            {
                var ldapSettingsDB = await _ldapSettingsRepository.GetLdapSettings();

                if (ldapSettingsDB == null)
                {
                    await _ldapSettingsRepository.Add(ldapSettings);
                    _notyfService.Success("Configurações LDAP salvas com sucesso.");
                }
                else
                {
                    await _ldapSettingsRepository.Update(ldapSettings);
                    _notyfService.Success("Configurações LDAP atualizadas com sucesso.");
                }

                return RedirectToAction("Ldap");
            }

            _notyfService.Warning("Preencha todos os campos obrigatórios.");
            return RedirectToAction("Ldap");
        }

        [HttpGet]
        public async Task<IActionResult> Email()
        {
            var ldapSettings = await _emailSettingsRepository.GetEmailSettings();

            if (ldapSettings == null)
            {
                return View();
            }

            return View(ldapSettings);
        }

        [HttpPost]
        public async Task<IActionResult> Email(EmailSettings emailSettings)
        {
            if (ModelState.IsValid)
            {
                var emailSettingsDB = await _emailSettingsRepository.GetEmailSettings();

                if (emailSettingsDB == null)
                {
                    await _emailSettingsRepository.Add(emailSettings);
                    _notyfService.Success("Configurações de E-mail salvas com sucesso.");
                }
                else
                {
                    await _emailSettingsRepository.Update(emailSettings);
                    _notyfService.Success("Configurações de E-mail atualizadas com sucesso.");
                }

                return RedirectToAction("Email");
            }

            _notyfService.Warning("Preencha todos os campos obrigatórios.");
            return RedirectToAction("Email");
        }
    }
}