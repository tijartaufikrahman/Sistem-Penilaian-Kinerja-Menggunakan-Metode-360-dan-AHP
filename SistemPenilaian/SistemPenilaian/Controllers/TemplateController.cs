using Microsoft.AspNetCore.Mvc;

namespace SistemPenilaian.Controllers
{
    public class TemplateController : Controller
    {
        [HttpGet]
        public IActionResult Table()
        {
            ViewData["Title"] = "Template Table";
            return View();
        }

        [HttpGet]
        public IActionResult Form()
        {
            ViewData["Title"] = "Template Form 1";
            return View();
        }

        [HttpGet]
        public IActionResult Form2()
        {
            ViewData["Title"] = "Template Form 2 - Pengaturan Jadwal Kerja";
            return View();
        }

        [HttpGet]
        public IActionResult Modal()
        {
            ViewData["Title"] = "Template Modal";
            return View();
        }

        [HttpGet]
        public IActionResult Card()
        {
            ViewData["Title"] = "Template Card";
            return View();
        }

        [HttpGet]
        public IActionResult Alert()
        {
            ViewData["Title"] = "Template Alert & Badge";
            return View();
        }
    }
}
