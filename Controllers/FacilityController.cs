using System.Drawing;
using System.Globalization;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618

namespace web_groupware.Controllers
{
    [Authorize]
    public class FacilityController : ScheduleFacilityController
    {
        public FacilityController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
        }
        
        [HttpGet]
        public IActionResult Index(string? start_date = null)
        {
            try
            {
                if (TempData["view_mode"] != null && TempData["view_mode"].ToString() == "week")
                {
                    return RedirectToAction("Week", new { start_date });
                }
                else
                {
                    return RedirectToAction("Day", new { start_date });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Week(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "week";
                ViewBag.ViewMode = "Week";
                var model = CreateViewModel(start_date);

                return View("Calendar", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Day(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "day";
                ViewBag.ViewMode = "Day";
                var model = CreateViewModel(start_date);

                return View("Calendar", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private ScheduleViewModel CreateViewModel(string? start_date)
        {
            try
            {
                var user_id = Convert.ToInt32(HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value);
                var me = _context.M_STAFF.Where(x => x.staf_cd == user_id).Select(x => new StaffModel
                {
                    staf_cd = x.staf_cd,
                    staf_name = x.staf_name
                }).FirstOrDefault();

                var viewModel = new ScheduleViewModel
                {
                    staf_cd = me.staf_cd,
                    staf_name = me.staf_name,
                    is_people = true,
                    // Keep previous week view when exit create/edit page
                    startDate = start_date ?? DateTime.Now.ToString("yyyy-MM-dd"),
                    PlaceList = _context.M_PLACE.OrderBy(x => x.sort).ToList()
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult CreateNormalWeek(string start_date, string? curr_date = null)
        {
            try
            {
                TempData["view_mode"] = "week";
                ViewBag.ViewMode = "Week";
                return CreateNormal(start_date, curr_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult CreateNormalDay(string start_date, string? curr_date = null)
        {
            try
            {
                TempData["view_mode"] = "day";
                ViewBag.ViewMode = "Day";
                return CreateNormal(start_date, curr_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private IActionResult CreateNormal(string start_date, string? curr_date = null, string? start_time = null, string? end_time = null)
        {
            try
            {
                var viewModel = CreateScheduleView(start_date, curr_date, start_time, end_time);
                return View("CreateNormal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNormal(ScheduleDetailViewModel request)
        {
            try
            {
                if (request.MyPlaceList.Length == 0)
                {
                    ModelState.AddModelError("", Messages.FACILITY_REQUIRED);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                var ret = await CreateSchedule(request);
                if (!ret)
                    return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }           

            var start_date = TempData["start_date"] != null ? DateTime.Parse(TempData["start_date"].ToString()).ToString("yyyy-MM-dd")
                        : DateTime.Now.ToString("yyyy-MM-dd");
            return Index(start_date);
        }

        [HttpGet]
        public IActionResult EditNormalDay(int schedule_no, string start_date, string? curr_date = null, string? start_time = null, string? end_time = null)
        {
            try
            {
                TempData["view_mode"] = "day";
                ViewBag.ViewMode = "Day";
                if (schedule_no == 0)
                {
                    return CreateNormal(start_date, curr_date, start_time, end_time);
                }
                else
                {
                    return EditNormal(schedule_no, start_date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult EditNormalWeek(int schedule_no, string start_date, string? curr_date = null)
        {
            try
            {
                TempData["view_mode"] = "week";
                ViewBag.ViewMode = "Week";
                if (schedule_no == 0)
                {
                    return CreateNormal(start_date, curr_date);
                }
                else
                {
                    return EditNormal(schedule_no, start_date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult EditNormal(int schedule_no, string start_date)
        {
            try
            {
                var viewModel = EditScheduleView(schedule_no, start_date);
                if (viewModel == null)
                {
                    return Index(start_date);
                }
                return View("EditNormal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditNormal(ScheduleDetailViewModel request)
        {
            try
            {
                if (request.MyPlaceList.Length == 0)
                {
                    ModelState.AddModelError("", Messages.FACILITY_REQUIRED);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                var ret = await UpdateSchedule(request);
                if (!ret)
                    return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }

            var start_date = TempData["start_date"] != null ? DateTime.Parse(TempData["start_date"].ToString()).ToString("yyyy-MM-dd")
                        : DateTime.Now.ToString("yyyy-MM-dd");
            return Index(start_date);
        }

         
        [HttpGet]
        public async Task<IActionResult> DeleteNormal(int schedule_no)
        {
            await DeleteSchedule(schedule_no);

            var start_date = TempData["start_date"] != null ? DateTime.Parse(TempData["start_date"].ToString()).ToString("yyyy-MM-dd")
                        : DateTime.Now.ToString("yyyy-MM-dd");
            return Index(start_date);
        }

        [HttpGet]
        public IActionResult FacilityList(int place, string keyword)
        {
            try
            {
                var placeList = place == 0 ? _context.M_PLACE.ToList() : _context.M_PLACE.Where(x => x.place_cd == place).ToList();
                if (placeList == null)
                {
                    return Json(new { status = "place empty" });
                }
                var user_id = Convert.ToInt32(HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value);
                var scheduleList = (from s in _context.T_SCHEDULE
                                    join l in _context.T_SCHEDULEPLACE on s.schedule_no equals l.schedule_no
                                    join m in _context.M_SCHEDULE_TYPE on s.schedule_type equals m.schedule_type
                                    let r = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == s.schedule_no)
                                    where !s.is_private || _context.T_SCHEDULEPEOPLE.Any(x => x.schedule_no == s.schedule_no && x.staf_cd == user_id)
                                    orderby l.place_cd, s.schedule_no, s.start_datetime
                                    select new
                                    {
                                        l.place_cd,
                                        s.schedule_no,
                                        typename = m.schedule_typename,
                                        typecolor = m.color,
                                        color = m.colorbk,
                                        start_datetime = (s.start_datetime != null) ? s.start_datetime.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                                        end_datetime = (s.end_datetime != null) ? s.end_datetime.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                                        title = s.title ?? "",
                                        memo = s.memo ?? "",
                                        s.schedule_type,
                                        s.is_private,
                                        repeat_type = r == null ? 0 : r.type,
                                        every_on = r == null ? 0 : r.every_on,
                                        time_from = r == null ? null : (r.time_from ?? null),
                                        time_to = r == null ? null : (r.time_to ?? null),
                                        repeat_date_from = r == null ? null : (r.date_from != null ? r.date_from.Value.ToString("yyyy-MM-dd") : null),
                                        repeat_date_to = r == null ? null : (r.date_to != null ? r.date_to.Value.ToString("yyyy-MM-dd") : null),
                                        _context.M_PLACE.Where(x => x.place_cd == l.place_cd).FirstOrDefault().duplicate
                                    })
                                    .ToList();
                if (place != 0)
                {
                    scheduleList = scheduleList.Where(x => x.place_cd == place).ToList();
                }
                if (keyword != null)
                {
                    scheduleList = scheduleList.Where(x => x.title.Contains(keyword) || x.memo.Contains(keyword)).ToList();
                }
                return Json(new { placeList, scheduleList });
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
    }
}
