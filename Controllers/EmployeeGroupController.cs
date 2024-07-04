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
    public class EmployeeGroupController : BaseController
    {
        public EmployeeGroupController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }

        // GET: T_GROUPM
        public async Task<IActionResult> Index()
        {
            try
            {
                EmployeeGroupDetailViewModel model = new EmployeeGroupDetailViewModel();
                var items = await _context.T_GROUPSTAFF.ToListAsync();
                foreach (var item in items)
                {
                    var t_STAFFM = await _context.M_STAFF
                        .FirstOrDefaultAsync(m => m.staf_cd.ToString() == item.update_user);
                    var t_GROUPSTAFF = await _context.M_STAFF
                        .FirstOrDefaultAsync(m => m.staf_cd == item.staf_cd);
                    var t_GROUPM = await _context.M_GROUP.FirstOrDefaultAsync(m => m.group_cd == item.group_cd);

                    model.empGroupList.Add(new EmployeeGroupDetail
                    {
                        group_cd = item.group_cd,
                        staf_cd = item.staf_cd,
                        staf_name = t_GROUPSTAFF != null ? t_GROUPSTAFF.staf_name : "",
                        group_name = t_GROUPM != null ? t_GROUPM.group_name : "",
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
        public IActionResult Create(int id)
        {
            try
            {
                var existingStaffIds = _context.T_GROUPSTAFF
                    .Where(gs => gs.group_cd == id)
                    .Select(gs => gs.staf_cd)
                    .ToList();

                var viewModel = new CreateViewModel
                {
                    GroupStaff = _context.M_GROUP.FirstOrDefault(gs => gs.group_cd == id),
                    StaffList = _context.M_STAFF
                        .Where(s => !existingStaffIds.Contains(s.staf_cd))
                        .ToList()
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(int id, CreateViewModel viewModel)
        {
            try
            {
                foreach (var stafCd in viewModel.SelectedStaffIds)
                {
                    if (_context.T_GROUPSTAFF.Any(d => d.group_cd == id && d.staf_cd == stafCd))
                    {
                        ModelState.AddModelError("SelectedStaffIds", $"The combination of staf_cd {stafCd} already exists.");
                        viewModel.StaffList = _context.M_STAFF.ToList();
                        return View(viewModel);
                    }
                    string update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                    var groupStaff = new T_GROUPSTAFF();
                    groupStaff.group_cd = id;
                    groupStaff.staf_cd = stafCd;
                    groupStaff.update_user = update_user;
                    groupStaff.update_date = DateTime.Now;
                    _context.Add(groupStaff);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("GetDetails", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(int? id)
        {
            try
            {
                var model = new EmployeeGroupDetailViewModel();
                var items = await _context.T_GROUPSTAFF.Where(m => m.group_cd == id).ToListAsync();
                foreach (var item in items)
                {
                    var t_STAFFM = await _context.M_STAFF
                        .FirstOrDefaultAsync(m => m.staf_cd.ToString() == item.update_user);
                    var t_GROUPSTAFF = await _context.M_STAFF
                        .FirstOrDefaultAsync(m => m.staf_cd == item.staf_cd);
                    var t_GROUPM = await _context.M_GROUP.FirstOrDefaultAsync(m => m.group_cd == item.group_cd);

                    model.empGroupList.Add(new EmployeeGroupDetail
                    {
                        group_cd = item.group_cd,
                        staf_cd = item.staf_cd,
                        staf_name = t_GROUPSTAFF != null ? t_GROUPSTAFF.staf_name : "",
                        group_name = t_GROUPM != null ? t_GROUPM.group_name : "",
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
        public async Task<IActionResult> Delete(int group_cd, int staf_cd)
        {
            try
            {
                var model = new EmployeeGroupDetail();
                var record = await _context.T_GROUPSTAFF.FirstOrDefaultAsync(m => m.group_cd == group_cd && m.staf_cd == staf_cd);
                var t_STAFFM = await _context.M_STAFF
                    .FirstOrDefaultAsync(m => m.staf_cd.ToString() == record.update_user);
                var t_GROUPSTAFF = await _context.M_STAFF
                    .FirstOrDefaultAsync(m => m.staf_cd == record.staf_cd);
                var t_GROUPM = await _context.M_GROUP.FirstOrDefaultAsync(m => m.group_cd == record.group_cd);
                model.group_cd = record.group_cd;
                model.staf_cd = record.staf_cd;
                model.staf_name = t_GROUPSTAFF != null ? t_GROUPSTAFF.staf_name : "";
                model.group_name = t_GROUPM != null ? t_GROUPM.group_name : "";
                model.update_user = t_STAFFM != null ? t_STAFFM.staf_name : "";
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
        public async Task<IActionResult> Delete(T_GROUPSTAFF model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_delete.FAILURE_001);
                    return View(model);
                }
                var record = await _context.T_GROUPSTAFF.FirstOrDefaultAsync(x => x.group_cd == model.group_cd && x.staf_cd == model.staf_cd);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しないグループコードです。");
                    return View(model);
                }
                _context.T_GROUPSTAFF.Remove(record);
                await _context.SaveChangesAsync();
                return RedirectToAction("GetDetails", new { id = model.group_cd });

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
