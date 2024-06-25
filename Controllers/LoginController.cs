using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using web_groupware.Data;
using web_groupware.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Controllers
{
    public class LoginController : BaseController
    {

        public LoginController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
        }

        public async Task<IActionResult> Index()
        {
            try
            {

                var model = new LoginViewModel();
                var t_INFO = await _context.T_INFO.Where(i => i.info_cd == 1).FirstOrDefaultAsync();
                if (t_INFO == null)
                {
                    model.title = "";
                    model.message = "";
                }
                else
                {
                    model.title = t_INFO.title;
                    model.message = t_INFO.message;
                }

                if (HttpContext.User.Claims.Count() != 0)
                {
                        return RedirectToAction("Index", "Home");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index([Bind("mail, password, remember")] LoginViewModel model)
        {
            try
            {
                var t_INFO = await _context.T_INFO.Where(i => i.info_cd == 1).FirstOrDefaultAsync();
                if (t_INFO == null)
                {
                    model.title = "";
                    model.message = "";
                }
                else
                {
                    model.title = t_INFO.title;
                    model.message = t_INFO.message;
                }

                string mail = model.mail;
                string? password = model.password;
                if (mail == null)
                {
                    return View(model);
                }
                var user_in_db = await _context.M_STAFF.FirstOrDefaultAsync(m => m.mail == mail);
                if (user_in_db == null)
                {
                    ModelState.AddModelError("", Messages.LOGIN_ERROR_MESSAGE01);
                    return View(model);
                }
                if (user_in_db.password == password)
                {
                    var name = user_in_db.staf_name;
                    var claims = new List<Claim>
                    {
                        new Claim(Utilities.ClaimTypes.STAF_CD, user_in_db.staf_cd.ToString()),
                        //new Claim(Utilities.ClaimTypes.STAF_NAME, user_in_db.staf_name ?? name),
                        //new Claim(Utilities.ClaimTypes.MAIL, user_in_db.mail),
                        //new Claim(Utilities.ClaimTypes.AUTH_ADMIN, user_in_db.auth_admin.ToString()),
                        //new Claim(Utilities.ClaimTypes.WORKFLOW_AUTH, user_in_db.workflow_auth.ToString()),
                    };

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.remember,
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, "AstIdentity");
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                    _logger.LogInformation(1, user_in_db.mail + " logged in.");
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    _logger.LogInformation(1, DateTime.Now.ToString() + user_in_db.mail + "Password is incorrect.");
                    ModelState.AddModelError("", Messages.LOGIN_ERROR_MESSAGE01);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation(2, " logged out.");
            return RedirectToAction("Index", "Login");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}