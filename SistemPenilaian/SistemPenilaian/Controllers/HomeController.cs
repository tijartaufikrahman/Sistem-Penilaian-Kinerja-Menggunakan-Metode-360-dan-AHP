using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SistemPenilaian.Models;

namespace SistemPenilaian.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Halaman Depan";
            return View();
        }

        [HttpGet]
        public IActionResult HalamanDepan()
        {
            ViewData["Title"] = "Halaman Depan";
            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
