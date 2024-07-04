using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class GroupController : BaseController
    {
        public GroupController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }
        // GET: T_GROUPM
        public async Task<IActionResult> Index()
        {
            GroupMasterViewModel model = new GroupMasterViewModel();
            var items = await _context.M_GROUP.ToListAsync();
            foreach (var item in items)
            {
                var t_STAFFM = await _context.M_STAFF
                    .FirstOrDefaultAsync(m => m.staf_cd.ToString() == item.update_user);

                // Count the number of users in the group
                var userCount = await _context.T_GROUPSTAFF
                    .Where(gs => gs.group_cd == item.group_cd)
                    .CountAsync();
                string update_date = item.update_date.ToString("yyyy/M/d HH:mm");

                model.groupList.Add(new GroupMasterDetailViewModel
                {
                    group_cd = item.group_cd,
                    group_name = item.group_name,
                    update_user = t_STAFFM.staf_name,
                    update_date = item.update_date,
                    user_count = userCount // Add the user count to the GroupMasterDetailViewModel model
                });
            }
            return View(model);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupMasterDetailViewModel model)
        {
            try
            {
                if (_context.M_GROUP.Any(d => d.group_cd == model.group_cd))
                {
                    ModelState.AddModelError("", "このグループコードは既に存在します。");
                    return View(model);
                }
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    return View(model);
                }

                var model_new = new M_GROUP();
                model_new.group_cd = model.group_cd;
                model_new.group_name = model.group_name;
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
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var record = await _context.M_GROUP.FirstOrDefaultAsync(m => m.group_cd == id);
                if (record == null)
                {
                    return RedirectToAction("Index");
                }
                var model = new GroupMasterDetailViewModel();
                model.group_cd = record.group_cd;
                model.group_name = record.group_name;
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
        public async Task<IActionResult> Edit(GroupMasterDetailViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_change.FAILURE_001);
                    return View(model);
                }
                var record = await _context.M_GROUP.FirstOrDefaultAsync(x => x.group_cd == model.group_cd);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しないグループコードです。");
                    return View(model);
                }
                record.group_cd=model.group_cd;
                record.group_name=model.group_name;
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var record = await _context.M_GROUP.FirstOrDefaultAsync(m => m.group_cd == id);
                if (record == null)
                {
                    return RedirectToAction("Index");
                }
                var t_STAFFM = await _context.M_STAFF.FirstOrDefaultAsync(m => m.staf_cd.ToString() == record.update_user);
                var model = new GroupMasterDetailViewModel();
                model.group_cd = record.group_cd;
                model.group_name = record.group_name;
                model.update_user = t_STAFFM.staf_name;
                model.update_date = record.update_date;
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
        public async Task<IActionResult> Delete(GroupMasterDetailViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_delete.FAILURE_001);
                    return View(model);
                }
                var record = await _context.M_GROUP.FirstOrDefaultAsync(x => x.group_cd == model.group_cd);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しないグループコードです。");
                    return View(model);
                }
                _context.M_GROUP.Remove(record);
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
