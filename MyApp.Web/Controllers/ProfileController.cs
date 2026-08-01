using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.DTOs;
using MyApp.Web.Services.Interfaces;
using MyApp.Web.ViewModels;

namespace MyApp.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;

    public ProfileController(IProfileService profileService, IAuthService authService)
    {
        _profileService = profileService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _profileService.GetProfileAsync();
        var model = new ProfileViewModel
        {
            Profile = response is { Success: true, Data: not null } ? response.Data : new()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var response = await _profileService.GetProfileAsync();
        if (response is null || !response.Success || response.Data is null)
            return NotFound();

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileDto model)
    {
        var dto = new UpdateProfileDto
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Designation = model.Designation,
            Department = model.Department,
            ProfilePicturePath = model.ProfilePicturePath
        };

        var response = await _profileService.UpdateProfileAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response?.Message ?? "Failed to update profile.");
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var dto = new ChangePasswordRequestDto
        {
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword
        };

        var response = await _authService.ChangePasswordAsync(dto);
        if (response is { Success: true })
        {
            model.Success = true;
            model.Message = "Password changed successfully.";
            return View(model);
        }

        model.Success = false;
        model.Message = response?.Message ?? "Failed to change password.";
        return View(model);
    }
}

[Authorize]
public class SettingsController : Controller
{
    private readonly IProfileService _profileService;

    public SettingsController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await _profileService.GetPreferencesAsync();
        var data = response is { Success: true, Data: not null } ? response.Data : new();
        var model = new SettingsViewModel
        {
            Theme = data.Theme,
            SidebarCollapsed = data.SidebarCollapsed,
            Language = data.Language
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        var dto = new UpdatePreferencesDto
        {
            Theme = model.Theme,
            SidebarCollapsed = model.SidebarCollapsed,
            Language = model.Language
        };

        var response = await _profileService.UpdatePreferencesAsync(dto);
        if (response is { Success: true })
        {
            TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        model.Message = response?.Message ?? "Failed to update settings.";
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Theme([FromBody] UpdatePreferencesDto dto)
    {
        var response = await _profileService.UpdatePreferencesAsync(dto);
        return Json(new { success = response?.Success ?? false });
    }
}
