using Microsoft.AspNetCore.Mvc;

namespace ProjectAW.Modules.Stocks.Controllers
{
    public class StockController : Controller
    {
        [HttpGet("/Stocks")]
        public IActionResult Index()
        {
            RouteData.Values["viewModule"] = "Stocks";
            return View();
        }
    }
}
