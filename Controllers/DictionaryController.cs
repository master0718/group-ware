using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618, CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class DictionaryController : BaseController
    {
        public DictionaryController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }
        
        // GET: T_DIC
        public async Task<IActionResult> Index()
        {
            try { 
                var model = new DictionaryDetailViewModel();
                var items = await _context.M_DIC.OrderBy(item => item.dic_cd).ThenBy(item => item.dic_kb).ToListAsync();
                foreach (var item in items)
                {
                    var t_STAFFM = await _context.M_STAFF
                        .FirstOrDefaultAsync(m => m.staf_cd.ToString() == item.update_user);

                    model.dicList.Add(new DictionaryDetail
                    {
                        dic_cd = item.dic_cd,
                        dic_kb = item.dic_kb,
                        content = item.content,
                        comment = item.comment,
                        update_user = t_STAFFM != null ? t_STAFFM.staf_name : "",
                        update_date = item.update_date
                    });
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

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DictionaryDetail model)
        {
            try
            {
                if (_context.M_DIC.Any(d => d.dic_kb == model.dic_kb && d.dic_cd == model.dic_cd))
                {
                    ModelState.AddModelError("", "この辞書コードと辞書区分の組み合わせは既に存在します。");
                    return View(model);
                }
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    return View(model);
                }
                var model_new = new M_DIC();
                model_new.dic_cd = model.dic_cd;
                model_new.dic_kb = model.dic_kb;
                model_new.content = model.content;
                model_new.comment = model.comment;
                model_new.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                model_new.update_date = DateTime.Now;
                _context.Add(model_new);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? dic_cd, int dic_kb)
        {
            try
            {
                var items = await _context.M_DIC.FirstOrDefaultAsync(m => m.dic_cd == dic_cd && m.dic_kb == dic_kb);
                if (items == null)
                {
                    return RedirectToAction("Index");
                }
                var model = new DictionaryDetail();
                model.dic_cd = items.dic_cd;
                model.dic_kb = items.dic_kb;
                model.content = items.content;
                model.comment = items.comment;
                model.update_user = items.update_user;
                model.update_date = items.update_date;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DictionaryDetail model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_change.FAILURE_001);
                    return View(model);
                }
                var record = await _context.M_DIC.FirstOrDefaultAsync(x => x.dic_cd == model.dic_cd && x.dic_kb == model.dic_kb);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しないグループコードです。");
                    return View(model);
                }
                record.dic_cd = model.dic_cd;
                record.dic_kb = model.dic_kb;
                record.content = model.content;
                record.comment = model.comment;
                record.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                record.update_date = DateTime.Now;
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string? dic_cd, int dic_kb)
        {
            try
            {
                var items = await _context.M_DIC.FirstOrDefaultAsync(m => m.dic_cd == dic_cd && m.dic_kb == dic_kb);
                if (items == null)
                {
                    return RedirectToAction("Index");
                }
                var model = new DictionaryDetail();
                model.dic_cd = items.dic_cd;
                model.dic_kb = items.dic_kb;
                model.content = items.content;
                model.comment = items.comment;
                model.update_user = items.update_user;
                model.update_date = items.update_date;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DictionaryDetail model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_delete.FAILURE_001);
                    return View(model);
                }
                var record = await _context.M_DIC.FirstOrDefaultAsync(x => x.dic_cd == model.dic_cd && x.dic_kb == model.dic_kb);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しないグループコードです。");
                    return View(model);
                }
                _context.M_DIC.Remove(record);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
    }
}
