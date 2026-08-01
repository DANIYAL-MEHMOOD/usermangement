using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ICurrentUserService _currentUser;

    public HomeController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public IActionResult Index()
    {
        if (_currentUser.IsAuthenticated)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return RedirectToAction("Login", "Auth");
    }
}
