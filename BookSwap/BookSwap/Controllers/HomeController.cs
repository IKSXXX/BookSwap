using Microsoft.AspNetCore.Mvc;

namespace BookExchange.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
