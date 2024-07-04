using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks.Dataflow;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618

namespace web_groupware.Controllers
{
    [Authorize]
    public class ScheduleController : ScheduleFacilityController
    {
        public ScheduleController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
        }

        [HttpGet]
        public IActionResult Index(string? start_date = null)
        {
            try
            {
                if (!TempData.ContainsKey("view_mode") || TempData["view_mode"].ToString() == "group_day")
                {
                    return RedirectToAction("GroupDay", new { start_date });
                }
                else if (TempData["view_mode"].ToString() == "group_week")
                {
                    return RedirectToAction("GroupWeek", new { start_date });
                }
                else if (TempData["view_mode"].ToString() == "person_month")
                {
                    return RedirectToAction("PersonMonth", new { start_date });
                }
                else if (TempData["view_mode"].ToString() == "person_week")
                {
                    return RedirectToAction("PersonWeek", new { start_date });
                }
                else if (TempData["view_mode"].ToString() == "person_month2")
                {
                    return RedirectToAction("PersonMonth2", new { start_date });
                }
                return RedirectToAction("GroupDay", new { start_date });
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult FilterList()
        {
            try
            {
                if (!TempData.ContainsKey("view_mode") || TempData["view_mode"].ToString().Contains("group"))
                {
                    var groupList = _context.M_GROUP.Select(x => new
                    {
                        x.group_cd,
                        x.group_name
                    }).ToList();

                    return Json(new { groupList });

                }
                else
                {
                    var staffList = _context.M_STAFF.Select(x => new
                    {
                        x.staf_cd,
                        x.staf_name
                    }).ToList();

                    return Json(new { staffList });
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
        public IActionResult GroupDay(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "group_day";
                ViewBag.ViewMode = "GroupDay";
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
        public IActionResult GroupWeek(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "group_week";
                ViewBag.ViewMode = "GroupWeek";
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
        public IActionResult PersonMonth(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "person_month";
                ViewBag.ViewMode = "PersonMonth";
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
        public IActionResult PersonMonth2(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "person_month2";
                ViewBag.ViewMode = "PersonMonth2";
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
        public IActionResult PersonWeek(string? start_date)
        {
            try
            {
                TempData["view_mode"] = "person_week";
                ViewBag.ViewMode = "PersonWeek";
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
                int my_staf_cd = Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                var me = _context.M_STAFF.Where(x => x.staf_cd == my_staf_cd).Select(x => new StaffModel
                {
                    staf_cd = x.staf_cd,
                    staf_name = x.staf_name
                }).FirstOrDefault();

                var viewModel = new ScheduleViewModel()
                {
                    staf_cd = me.staf_cd,
                    staf_name = me.staf_name,
                    is_people = true,
                    // Keep previous week view when exit create/edit page
                    startDate = start_date ?? DateTime.Now.ToString("yyyy-MM-dd")
                };

                if (!TempData.ContainsKey("view_mode") || TempData["view_mode"].ToString().Contains("group"))
                {
                    var groupList = _context.M_GROUP.Select(x => new EmployeeGroupModel
                    {
                        group_cd = x.group_cd,
                        group_name = x.group_name
                    }).ToList();
                    viewModel.GroupList = groupList;
                }
                else
                {
                    var staffList = _context.M_STAFF.Select(x => new StaffModel
                    {
                        staf_cd = x.staf_cd,
                        staf_name = x.staf_name
                    }).ToList();
                    viewModel.StaffList = staffList;
                }

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
        public IActionResult CreateGroupWeek(string start_date)
        {
            try
            {
                TempData["view_mode"] = "group_week";
                ViewBag.ViewMode = "GroupWeek";
                return Create(start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult CreateGroupDay(string start_date)
        {
            try
            {
                TempData["view_mode"] = "group_day";
                ViewBag.ViewMode = "GroupDay";
                return Create(start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult CreatePersonWeek(string start_date)
        {
            try
            {
                TempData["view_mode"] = "person_week";
                ViewBag.ViewMode = "PersonWeek";
                return Create(start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult CreatePersonMonth(string start_date)
        {
            try
            {
                TempData["view_mode"] = "person_month";
                ViewBag.ViewMode = "PersonMonth";
                return Create(start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult CreatePersonMonth2(string start_date)
        {
            try
            {
                TempData["view_mode"] = "person_month2";
                ViewBag.ViewMode = "PersonMonth2";
                return Create(start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Create(string start_date)
        {
            try
            {
                var viewModel = CreateScheduleView(start_date);
                return View("Create", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }            
        }

        [HttpPost]
        public async Task<IActionResult> Create(ScheduleDetailViewModel request)
        {
            try
            {
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
        public IActionResult EditGroupDay(int schedule_no, string start_date)
        {
            try
            {
                TempData["view_mode"] = "group_day";
                ViewBag.ViewMode = "GroupDay";
                return Edit(schedule_no, start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult EditGroupWeek(int schedule_no, string start_date)
        {
            try
            {
                TempData["view_mode"] = "group_week";
                ViewBag.ViewMode = "GroupWeek";
                return Edit(schedule_no, start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult EditPersonWeek(int schedule_no, string start_date)
        {
            try
            {
                TempData["view_mode"] = "person_week";
                ViewBag.ViewMode = "PersonWeek";
                return Edit(schedule_no, start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult EditPersonMonth(int schedule_no, string start_date)
        {
            try
            {
                TempData["view_mode"] = "person_month";
                ViewBag.ViewMode = "PersonMonth";
                return Edit(schedule_no, start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult EditPersonMonth2(int schedule_no, string start_date)
        {
            try
            {
                TempData["view_mode"] = "person_month2";
                ViewBag.ViewMode = "PersonMonth2";
                return Edit(schedule_no, start_date);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Edit(int schedule_no, string start_date)
        {
            try
            {
                var viewModel = EditScheduleView(schedule_no, start_date);
                if (viewModel == null)
                {
                    return Index(start_date);
                }
                return View("Edit", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }            
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ScheduleDetailViewModel request)
        {
            try
            {
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
        public async Task<IActionResult> Delete(int schedule_no)
        {
            await DeleteSchedule(schedule_no);

            var start_date = TempData["start_date"] != null ? DateTime.Parse(TempData["start_date"].ToString()).ToString("yyyy-MM-dd")
                        : DateTime.Now.ToString("yyyy-MM-dd");
            return Index(start_date);
        }

        [HttpGet]
        public IActionResult ScheduleListGroup(string filter)
        {
            try
            {
                int user_id = Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);

                List<int> staffs_in_group = new()
                {
                    user_id
                };

                int group_cd = 0;
                if (filter != null)
                {
                    group_cd = Convert.ToInt32(filter);
                    if (group_cd > 0)
                    {
                        staffs_in_group = staffs_in_group.Union(from gs in _context.T_GROUPSTAFF
                                                                where gs.group_cd == group_cd
                                                                select gs.staf_cd).ToList();
                    }
                }

                var schedulePeople = (from p in _context.T_SCHEDULEPEOPLE
                                      where group_cd == 0 || staffs_in_group.Contains(p.staf_cd)
                                      select new { p.schedule_no, p.staf_cd });
                                    //  .Union(
                                    //from g in _context.T_SCHEDULEGROUP
                                    //join gs in _context.T_GROUPSTAFF on g.group_cd equals gs.group_cd
                                    //where group_cd == 0 || staffs_in_group.Contains(gs.staf_cd)
                                    //select new { g.schedule_no, gs.staf_cd });

                var scheduleList = (from s in _context.T_SCHEDULE
                                    join p in schedulePeople on s.schedule_no equals p.schedule_no
                                    let r = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == s.schedule_no)
                                    join u in _context.M_STAFF on p.staf_cd equals u.staf_cd
                                    join m in _context.M_SCHEDULE_TYPE on s.schedule_type equals m.schedule_type
                                    orderby s.schedule_no, s.start_datetime
                                    select new
                                    {
                                        s.schedule_no,
                                        u.staf_cd,
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
                                        repeat_date_to = r == null ? null : (r.date_to != null ? r.date_to.Value.ToString("yyyy-MM-dd") : null)
                                    }).ToList();

                var staffList = (from s in _context.M_STAFF
                                 from gs in _context.T_GROUPSTAFF.Where(x => x.staf_cd == s.staf_cd).DefaultIfEmpty()
                                 where group_cd == 0 || gs.group_cd == group_cd || s.staf_cd == user_id
                                 select new { s.staf_cd, s.staf_name }).ToList();

                scheduleList = scheduleList.Where(x => !x.is_private || x.staf_cd == user_id).ToList();

                return Json(new { user_id, staffList, scheduleList });
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult ScheduleListPerson(string filter)
        {
            try
            {
                var filter_user_id = 0;
                var user_id = Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                if (filter != null)
                {
                    filter_user_id = Convert.ToInt32(filter);
                }
                else
                {
                    filter_user_id = user_id;
                }

                var schedulePeople = (from p in _context.T_SCHEDULEPEOPLE
                                      where p.staf_cd == filter_user_id
                                      select new { p.schedule_no, p.staf_cd });
                                    //  .Union(
                                    //from g in _context.T_SCHEDULEGROUP
                                    //join gs in _context.T_GROUPSTAFF on g.group_cd equals gs.group_cd
                                    //where gs.staf_cd == filter_user_id
                                    //select new { g.schedule_no, gs.staf_cd });

                var scheduleList = (from s in _context.T_SCHEDULE
                                    join p in schedulePeople on s.schedule_no equals p.schedule_no
                                    let r = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == s.schedule_no)
                                    join u in _context.M_STAFF on p.staf_cd equals u.staf_cd
                                    join m in _context.M_SCHEDULE_TYPE on s.schedule_type equals m.schedule_type
                                    where p.staf_cd == filter_user_id
                                    orderby s.schedule_no, s.start_datetime
                                    select new
                                    {
                                        s.schedule_no,
                                        u.staf_cd,
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
                                        repeat_date_to = r == null ? null : (r.date_to != null ? r.date_to.Value.ToString("yyyy-MM-dd") : null)
                                    }).ToList();

                scheduleList = scheduleList.Where(x => !x.is_private || x.staf_cd == user_id).ToList();

                return Json(new { scheduleList });
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