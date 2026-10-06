using Microsoft.AspNetCore.Mvc;

namespace SistemPenilaian.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }

        [HttpGet]
        public IActionResult Karyawan()
        {
            ViewData["Title"] = "Data Karyawan";
            return View();
        }

        [HttpGet]
        public IActionResult MasterKategori()
        {
            ViewData["Title"] = "Kategori & Rekomendasi";
            return View();
        }

        [HttpGet]
        public IActionResult MasterKriteria()
        {
            ViewData["Title"] = "Kriteria Penilaian";
            return View();
        }

        [HttpGet]
        public IActionResult MasterBobotAHP()
        {
            ViewData["Title"] = "Pembobotan Kriteria";
            return View();
        }

        [HttpGet]
        public IActionResult Periode()
        {
            ViewData["Title"] = "Penilaian Kinerja";
            return View();
        }

        [HttpGet]
        public IActionResult TambahPeriode()
        {
            ViewData["Title"] = "Tambah Periode Penilaian";
            return View();
        }


        [HttpGet]
        public IActionResult HasilPenilaian()
        {
            ViewData["Title"] = "Hasil Evaluasi Kinerja";
            return View();
        }

        [HttpGet]
        public IActionResult RiwayatPenilaian()
        {
            ViewData["Title"] = "Riwayat Penilaian";
            return View();
        }

        [HttpGet]
        public IActionResult HistoryPenilaian()
        {
            return RedirectToAction(nameof(RiwayatPenilaian));
        }

        [HttpGet]
        public IActionResult Database()
        {
            ViewData["Title"] = "Kelola Akses & Pengguna";
            return View();
        }
    }
}
