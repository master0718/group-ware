using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class NoticeLoginController : BaseController
    {
        const int info_cd = 1;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public NoticeLoginController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }

        /// <summary>
        /// 初期表示
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                //画面に渡すモデル作成
                var model = new NoticeLoginViewModel();
                var t_info = _context.T_INFO.FirstOrDefault(x => x.info_cd == info_cd);
                if (t_info != null)
                {
                    model.title = t_info.title;
                    model.message = t_info.message;

                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return RedirectToAction("Index", "Login");
            }
        }

        /// <summary>
        /// 登録・変更
        /// </summary>
        /// <param name="file_name">NoticeViewModel</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoticeLoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    return View("Index", model);
                }

                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;
                        //T_INFO　登録・変更
                        var recoard = await _context.T_INFO.FirstOrDefaultAsync(x => x.info_cd == info_cd);
                        if (recoard == null)
                        {
                            var recoard_new = new T_INFO();
                            recoard_new.info_cd = info_cd;
                            recoard_new.title = model.title;
                            recoard_new.message = model.message;
                            recoard_new.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                            recoard_new.create_date = now;
                            recoard_new.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                            recoard_new.update_date = now;
                            await _context.T_INFO.AddAsync(recoard_new);
                            await _context.SaveChangesAsync();

                        }
                        else
                        {
                            recoard.title = model.title;
                            recoard.message = model.message;
                            recoard.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                            recoard.create_date = now;
                            recoard.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                            recoard.update_date = now;
                            await _context.SaveChangesAsync();
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        _logger.LogError(ex.Message);
                        _logger.LogError(ex.StackTrace);
                        tran.Dispose();
                        ModelState.AddModelError("", Message_register.FAILURE_001);
                        return View("Index", model);
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                ModelState.AddModelError("", Message_register.FAILURE_001);
                return View("Index", model);
            }
        }
    }
}
