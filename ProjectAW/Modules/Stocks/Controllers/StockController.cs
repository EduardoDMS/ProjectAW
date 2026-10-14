using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Stocks.Controllers
{
    public class StockController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
