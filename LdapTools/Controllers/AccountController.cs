using AspNetCoreHero.ToastNotification.Abstractions;
using LdapTools.Models;
using LdapTools.Repositories.Interfaces;
using LdapTools.Services;
using LdapTools.Services.Interfaces;
using LdapTools.VewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace LdapTools.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILdapService _ldapService;
        private readonly IEmailSender _emailSender;
        private readonly TokenService _tokenService;
        private readonly INotyfService _notyfService;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILdapService ldapService, 
            IEmailSender emailSender, 
            TokenService tokenService,
            INotyfService notyfService,
            IPasswordResetTokenRepository resetTokenRepository)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _ldapService = ldapService;
            _emailSender = emailSender;
            _tokenService = tokenService;
            _notyfService = notyfService;
            _passwordResetTokenRepository = resetTokenRepository;
        }

		[HttpGet]
		public IActionResult Login()
		{
			if (User.Identity != null && User.Identity.IsAuthenticated)
			{
				return RedirectToAction("Index", "Home");
			}

			var model = new LoginViewModel();

			var rememberedUsername = Request.Cookies["RememberedUsername"];
			if (!string.IsNullOrEmpty(rememberedUsername))
			{
				model.Username = rememberedUsername;
			}

			return View(model);
		}

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var username = model.Username;
                var password = model.Password;
                var rememberMe = model.RememberMe;

                if (username != null && password != null)
                {
                    if (username == "admin")
                    {
                        var result = await _signInManager.PasswordSignInAsync(username, password, rememberMe, false);

                        if (result.Succeeded)
                        {
                            if (rememberMe)
                            {
                                Response.Cookies.Append("RememberedUsername", username, new CookieOptions
                                {
                                    Expires = DateTime.UtcNow.AddDays(30),
                                    HttpOnly = true,
                                    SameSite = SameSiteMode.Lax
                                });
                            }
                            else
                            {
                                Response.Cookies.Delete("RememberedUsername");
                            }

                            Log.Information("O usuário {User} realizou o login no sistema em {Timestamp}",
                                username, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                            return RedirectToAction("Index", "Home");
                        }

                        ModelState.AddModelError(string.Empty, "Usuário ou senha incorreto.");

                        Log.Warning("Tentativa de login mal sucedida com o usuário {User} em {Timestamp}",
                                username, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                        return View(model);
                    }

                    if (await _ldapService.IsAuthenticated(username, password))
                    {
                        var user = await _userManager.FindByNameAsync(username);
                        if (user == null)
                        {
                            ModelState.AddModelError(string.Empty, "Usuário não tem permissão para acessar o sistema.");
                            return View(model);
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false);

                        if (model.RememberMe)
                        {
                            Response.Cookies.Append("RememberedUsername", username, new CookieOptions
                            {
                                Expires = DateTime.UtcNow.AddDays(30),
                                HttpOnly = true,
                                SameSite = SameSiteMode.Lax
                            });
                        }
                        else
                        {
                            Response.Cookies.Delete("RememberedUsername");
                        }

                        Log.Information("O usuário {User} realizou o login no sistema em {Timestamp}",
                                username, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Usuário ou senha incorreto.");

                Log.Warning("Tentativa de login mal sucedida com o usuário {User} em {Timestamp}",
                                username, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePassword(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && (user.UserType != "LOCAL" || user.Id != id))
            {
                return Json(new { message = "Usuário incorreto!" });
            }

            var changePasswordView = new ChangePasswordViewModel
            {
                UserId = id
            };

            return PartialView("_ChangePassword", changePasswordView);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentPassword = model.CurrentPassword;
                var newPassword = model.NewPassword;

                if (currentPassword == null || newPassword == null)
                {
                    _notyfService.Error("Preencha todos os campos obrigarórios.");
                    return RedirectToAction("Profile");
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, currentPassword);
                if (passwordValid)
                {
                    if (model.NewPassword == model.ConfirmPassword)
                    {
                        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                        if (result.Succeeded)
                        {
                            await _signInManager.RefreshSignInAsync(user);
                            _notyfService.Success("Senha alterada com sucesso.");
                        }

                        foreach (var error in result.Errors)
                        {
                            _notyfService.Error(error.Description);
                        }
                    }
                    else
                    {
                        _notyfService.Error("A nova senha e a confirmação da senha não coincidem.");
                    }
                }
                else
                {
                    _notyfService.Error("A senha atual está incorreta.");
                }

                return RedirectToAction("Profile");
            }

            _notyfService.Error("Usuário não encontrado.");

            return RedirectToAction("Profile");
        }

        public ActionResult ForgotPassword()
        {
            var forgotPasswordViewModel = new ForgotPasswordViewModel
            {
                FortigateLogin = "false"
            };

            return View(forgotPasswordViewModel);
        }

        [HttpGet("/Account/ForgotPassword/{fortigateLogin}")]
		public ActionResult ForgotPassword(string fortigateLogin)
        {
            var forgotPasswordViewModel = new ForgotPasswordViewModel
            {
                FortigateLogin = fortigateLogin
            };

            return View(forgotPasswordViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
        {
            var username = forgotPasswordViewModel.Username;
            var fortigateLogin = forgotPasswordViewModel.FortigateLogin;

            if (username == null || fortigateLogin == null) return RedirectToAction("ExpirationToken");

            var email = await _ldapService.GetEmailByUsernameAsync(username);
            if (email == null) return View("Index");

            var token = _tokenService.GenerateToken();
            var hashedToken = _tokenService.HashToken(token);

            var expirationTime = DateTime.UtcNow.AddHours(1);

            await _passwordResetTokenRepository.SavePasswordResetTokenAsync(username, email, hashedToken, fortigateLogin, expirationTime);
            
            await _emailSender.SendPasswordResetEmailAsync(email, token);

            _notyfService.Success("Foi enviado um e-mail com o link para recuperação da senha.");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var hashedToken = _tokenService.HashToken(token);

            var storedToken = await _passwordResetTokenRepository.GetPasswordResetTokenAsync(hashedToken);
            if (storedToken == null)
            {
                return RedirectToAction("ExpirationToken");
            }

            var ldapUser = await _ldapService.GetUserByEmailAsync(storedToken.Email);

            var model = new ResetPasswordViewModel
            {
                Name = ldapUser.FullName,
                Email = storedToken.Email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _ldapService.ResetPasswordAsync(model.Email, model.NewPassword);
            if (!result)
            {
                ModelState.AddModelError("", "Erro ao redefinir a senha.");
                return View(model);
            }

            var hashedToken = _tokenService.HashToken(model.Token);
            var storedToken = await _passwordResetTokenRepository.GetPasswordResetTokenAsync(hashedToken);
            var fortigateLogin = storedToken.FortigateLogin;

            await _passwordResetTokenRepository.RemovePasswordResetTokenAsync(storedToken.Id);
            
            if (fortigateLogin == "false")
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            _notyfService.Success("Redirecionando para tela de login...", 3);
            return RedirectToAction("ResetPasswordConfirmationFortigate");
        }

        [HttpGet]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ResetPasswordConfirmationFortigate()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ExpirationToken()
        {
            return View();
        }
    }
}