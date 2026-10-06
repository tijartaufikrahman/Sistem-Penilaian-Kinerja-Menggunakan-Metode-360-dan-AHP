using Microsoft.AspNetCore.Mvc;

namespace SistemPenilaian.Controllers
{
    public class KaryawanController : Controller
    {
        [HttpGet]
        public IActionResult Beranda()
        {
            ViewData["Title"] = "Beranda";
            return View("Dashboard");
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Beranda";
            return View();
        }

        [HttpGet]
        [Route("Karyawan/KinerjaPribadi")]
        [Route("Karyawan/RaporKinerja")]
        public IActionResult KinerjaPribadi()
        {
            ViewData["Title"] = "Rapor Kinerja Pribadi";
            return View();
        }

        [HttpGet]
        [Route("Karyawan/PenilaianKinerja")]
        [Route("Karyawan/Evaluasi360")]
        public IActionResult PenilaianKinerja()
        {
            ViewData["Title"] = "Penilaian Rekan Kerja";
            return View();
        }

        [HttpGet]
        [Route("Karyawan/HistoryPenilaian")]
        [Route("Karyawan/RiwayatEvaluasi")]
        public IActionResult HistoryPenilaian()
        {
            return RedirectToAction("PenilaianKinerja");
        }
    }
}
