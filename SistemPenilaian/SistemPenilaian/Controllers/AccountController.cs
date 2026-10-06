using Microsoft.AspNetCore.Mvc;

namespace SistemPenilaian.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        [Route("")]
        [Route("Account/Login")]
        [Route("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        [Route("Account/ForgotPassword")]
        [Route("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        [Route("Account/ForgotPasswordConfirmation")]
        [Route("ForgotPasswordConfirmation")]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Route("Account/VerifyOtp")]
        [Route("VerifyOtp")]
        public IActionResult VerifyOtp(string? email = null)
        {
            ViewData["Email"] = email ?? "";
            return View();
        }

        [HttpGet]
        [Route("Account/ResetPassword")]
        [Route("ResetPassword")]
        public IActionResult ResetPassword(string? token = null, string? email = null, string? otp = null)
        {
            ViewData["Token"] = token ?? "";
            ViewData["Email"] = email ?? "";
            ViewData["Otp"] = otp ?? "";
            return View();
        }
    }
}
