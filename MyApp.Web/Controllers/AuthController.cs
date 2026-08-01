using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_currentUser.IsAuthenticated)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new LoginRequestDto
        {
            Username = model.Username,
            Password = model.Password,
            RememberMe = model.RememberMe
        };

        var response = await _authService.LoginAsync(dto);

        if (response is null || !response.Success || response.Data is null)
        {
            model.ErrorMessage = response?.Message ?? "Invalid username or password.";
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.Data.UserId.ToString()),
            new(ClaimTypes.Name, response.Data.Username),
            new(ClaimTypes.GivenName, response.Data.FullName),
            new(ClaimTypes.Role, response.Data.RoleName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = response.Data.AccessTokenExpiry
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        await _authService.LogoutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new ForgotPasswordRequestDto { Email = model.Email };
        var response = await _authService.ForgotPasswordAsync(dto);

        model.Success = response?.Success ?? false;
        model.Message = response?.Message ?? "If your email is registered, you will receive a password reset link.";
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token = null)
    {
        return View(new ResetPasswordViewModel { Token = token ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new ResetPasswordRequestDto
        {
            Token = model.Token,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword
        };

        var response = await _authService.ResetPasswordAsync(dto);
        model.Success = response?.Success ?? false;
        model.Message = response?.Message ?? (model.Success ? "Password reset successfully." : "Failed to reset password.");
        return View(model);
    }
}
