using Fixtrack.Data;
using Fixtrack.Models;
using Fixtrack.Services;
using Fixtrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Fixtrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IAppEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IAppEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }


        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the account using the email entered by the user.
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password."
                );

                return View(model);
            }

            // Prevent deactivated staff accounts from logging in.
            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been deactivated."
                );

                return View(model);
            }

            // Let ASP.NET Identity verify the password and create
            // the authentication cookie.
            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                // Login was successful.
                _logger.LogInformation("User {Email} logged in.", user.Email);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(
                "",
                "Invalid email or password."
            );

            return View(model);
        }


        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }


        // =========================
        // FORGOT PASSWORD (email reset link)
        // =========================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Send only if the account exists AND is active, but always show the
            // same confirmation page so nobody can discover which emails exist.
            if (user != null && user.IsActive && !string.IsNullOrEmpty(user.Email))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var link = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { email = user.Email, token },
                    Request.Scheme);

                var body = $@"
                    <p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
                    <p>We received a request to reset your FixTrack password.
                       Click the link below to choose a new one:</p>
                    <p><a href=""{link}"">Reset my password</a></p>
                    <p>If you did not ask for this, ignore this email.
                       Your password will not change.</p>";

                await _emailSender.SendAsync(
                    user.Email,
                    "Reset your FixTrack password",
                    body);

                _logger.LogInformation("Password reset email requested for {Email}.", user.Email);
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        // =========================
        // RESET PASSWORD (from the emailed link)
        // =========================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid password reset link.");
            }

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Don't reveal whether the account exists or is deactivated.
            if (user == null || !user.IsActive)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset completed for {Email}.", user.Email);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}