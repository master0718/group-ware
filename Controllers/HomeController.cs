using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using web_groupware.Models;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Data;
using Microsoft.AspNetCore.StaticFiles;

namespace web_groupware.Controllers
{
    public class HomeController : BaseController
    {

        public HomeController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { 
        }
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }
        //close colorbox and reload parent window
        [Authorize]
        public IActionResult Back_to_parent()
        {
            return View();
        }

        [HttpPost]
        public IActionResult View_list_detail(string param1, string param2)
        {
            return View();
        }

        [Authorize]
        /// <summary>
        /// 画像表示ページを表示
        /// </summary>
        /// <param name="model">HomeShowImageViewModel</param>
        /// <returns>HomeShowImageViewModel</returns>
        public IActionResult ShowImage(HomeShowImageViewModel model)
        {
            model.Src = "GetImage?path=" + model.Path;
            return View(model);
        }

        [Authorize]
        [HttpGet]
        /// <summary>
        /// 画像イメージ取得
        /// </summary>
        /// <param name="path">フルパス</param>
        /// <returns>画像ファイル(ストリーム)</returns>
        public IActionResult GetImage(string path)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(path, out string contentType);
            contentType ??= System.Net.Mime.MediaTypeNames.Application.Octet;
            MemoryStream ms = new MemoryStream();
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                fs.CopyTo(ms);
            }
            ms.Position = 0;
            return File(ms, contentType, Path.GetFileName(path));

        }
    }
}