using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618, CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class EmployeeController : BaseController
    {
        public EmployeeController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var model=new EmployeeListViewModel();
                var list_t_staff = await _context.M_STAFF.ToListAsync();
                for(int i = 0; i < list_t_staff.Count;i++)
                {
                    var model_child = new EmployeeViewModel();
                    model_child.staf_cd = list_t_staff[i].staf_cd;
                    model_child.password = list_t_staff[i].password;
                    model_child.staf_name = list_t_staff[i].staf_name;
                    model_child.mail = list_t_staff[i].mail;
                    var t_staff_auth = await _context.M_STAFF_AUTH.FirstOrDefaultAsync(x => x.staf_cd == list_t_staff[i].staf_cd);
                    model_child.auth_admin = t_staff_auth == null ? 0: t_staff_auth.auth_admin;
                    model_child.workflow_auth = t_staff_auth == null ? 0 : t_staff_auth.workflow_auth;
                    model.list_employee.Add(model_child);
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
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                var record = await _context.M_STAFF.FirstOrDefaultAsync(m => m.staf_cd == id);
                if (record == null)
                {
                    return RedirectToAction("Index");
                }
                var record_auth = await _context.M_STAFF_AUTH.FirstOrDefaultAsync(m => m.staf_cd == id);

                var model = new EmployeeViewModel();
                model.staf_cd = record.staf_cd;
                model.staf_name = record.staf_name;
                model.password = record.password;
                model.mail = record.mail;
                model.auth_admin =record_auth == null ? 0 : record_auth.auth_admin;
                model.workflow_auth = record_auth == null ? 0 : record_auth.workflow_auth;
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
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_change.FAILURE_001);
                    return View(model);
                }
                var record = await _context.M_STAFF.FirstOrDefaultAsync(x => x.staf_cd == model.staf_cd);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しない社員番号です。");
                    return View(model);
                }
                var record_auth = await _context.M_STAFF_AUTH.FirstOrDefaultAsync(x => x.staf_cd == model.staf_cd);
                var now= DateTime.Now;

                if(record_auth == null)
                {
                    var record_auth_new = new M_STAFF_AUTH();
                    record_auth_new.staf_cd = model.staf_cd;
                    record_auth_new.auth_admin = model.auth_admin;
                    record_auth_new.workflow_auth = model.workflow_auth;
                    record_auth_new.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                    record_auth_new.update_date = now;
                    _context.M_STAFF_AUTH.Add(record_auth_new);
                }
                else
                {
                    record_auth.auth_admin = model.auth_admin;
                    record_auth.workflow_auth = model.workflow_auth;
                    record_auth.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                    record_auth.update_date = now;

                }

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

        //[HttpGet]
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    try
        //    {
        //        var record = await _context.M_STAFF.FirstOrDefaultAsync(m => m.staf_cd == id);
        //        if (record == null)
        //        {
        //            return RedirectToAction("Index");
        //        }
        //        var record_auth = await _context.M_STAFF_AUTH.FirstOrDefaultAsync(m => m.staf_cd == id);

        //        var model = new EmployeeViewModel();
        //        model.staf_cd = record.staf_cd;
        //        model.staf_name = record.staf_name;
        //        model.password = record.password;
        //        model.mail = record.mail;
        //        model.auth_admin = record_auth == null ? 0 : record_auth.auth_admin;
        //        model.workflow_auth = record_auth == null ? 0 : record_auth.workflow_auth;
        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.Message);
        //        _logger.LogError(ex.StackTrace);
        //        throw;
        //    }
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(EmployeeViewModel model)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            ModelState.AddModelError("", Message_delete.FAILURE_001);
        //            return View(model);
        //        }
        //        var record = await _context.M_STAFF.FirstOrDefaultAsync(x => x.staf_cd == model.staf_cd);
        //        if (record == null)
        //        {
        //            ModelState.AddModelError("", "存在しない社員番号です。");
        //            return View(model);
        //        }
        //        _context.M_STAFF.Remove(record);

        //        var record_auth = await _context.M_STAFF_AUTH.FirstOrDefaultAsync(x => x.staf_cd == model.staf_cd);
        //        if(record_auth != null)
        //        {
        //            _context.M_STAFF_AUTH.Remove(record_auth);
        //        }

        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex.Message);
        //        _logger.LogError(ex.StackTrace);
        //        throw;
        //    }
        //}
    }
}
