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
    public class ScheduleFacilityController : BaseController
    {
        private readonly string _uploadPath;

        public ScheduleFacilityController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
            var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.SCHEDULE);
            if (t_dic == null || t_dic.content == null)
            {
                _logger.LogError(Messages.ERROR_PREFIX + Messages.DICTIONARY_FILE_PATH_NO_EXIST, DIC_KB.SAVE_PATH_FILE, DIC_KB_700_DIRECTORY.SCHEDULE);
                throw new Exception(Messages.DICTIONARY_FILE_PATH_NO_EXIST);
            }
            else
            {
                _uploadPath = t_dic.content;
            }
        }

        protected ScheduleDetailViewModel CreateScheduleView(string start_date, string? curr_date = null)
        {
            var viewModel = new ScheduleDetailViewModel();
            PrepareViewModel(viewModel);

            var now = DateTime.Now;

            if (curr_date == null)
            {
                viewModel.start_datetime = DateTime.Now.ToString("yyyy-MM-dd");
                viewModel.end_datetime = DateTime.Now.ToString("yyyy-MM-dd");
            }
            else
            {
                viewModel.start_datetime = curr_date;
                viewModel.end_datetime = curr_date;
            }
            viewModel.is_private = false;

            var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
            viewModel.MyStaffList = new string[1];
            viewModel.MyStaffList[0] = "S-" + user_id;

            string dir_work = Path.Combine("work", user_id, now.ToString("yyyy_MM_dd_HH_mm_ss"));
            string dir = Path.Combine(_uploadPath, dir_work);
            //workディレクトリの作成
            Directory.CreateDirectory(dir);
            viewModel.work_dir = dir_work;
            viewModel.fileModel.editable = 1;

            ViewBag.StartDate = start_date;
            TempData["start_date"] = start_date;

            return viewModel;
        }

        protected async Task<bool> CreateSchedule(ScheduleDetailViewModel request)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                PrepareViewModel(request);
                return false;
            }

            // check if duplicated
            foreach (var item in request.MyPlaceList)
            {
                if (HasAlreadyReservedPlace(item, request))
                {
                    ModelState.AddModelError("", Messages.PLACE_ALREADY_RESERVED);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }
            }

            if (request.File.Count > 5)
            {
                ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                PrepareViewModel(request);

                return false;
            }

            var list_allowed_file_extentions = new List<string>() { ".pdf" };
            for (int i = 0; i < request.File.Count; i++)
            {
                if (!list_allowed_file_extentions.Contains(Path.GetExtension(request.File[i].FileName).ToLower()))
                {
                    ModelState.AddModelError("", Messages.BOARD_ALLOWED_FILE_EXTENSIONS);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }
                if (request.File[i].Length > Format.FILE_SIZE)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }
            }

            // in case of copy
            var schedule_no_origin = request.schedule_no;

            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var user_id = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    var schedule_no = GetNextNo(DataTypes.SCHEDULE_NO);
                    var now = DateTime.Now;

                    // add T_SCHEDULE
                    var schedule = new T_SCHEDULE()
                    {
                        schedule_no = schedule_no,
                        schedule_type = request.schedule_type,
                        title = request.title,
                        memo = request.memo,
                        is_private = request.is_private,
                    };

                    if (request.repeat_type == SCHEDULE_REPETITION.NONE)
                    {
                        if (request.start_datetime != null)
                        {
                            schedule.start_datetime = DateTime.Parse(request.start_datetime);
                        }
                        if (request.end_datetime != null)
                        {
                            schedule.end_datetime = DateTime.Parse(request.end_datetime);
                        }
                    }
                    schedule.create_user = user_id;
                    schedule.create_date = now;
                    schedule.update_user = schedule.create_user;
                    schedule.update_date = schedule.create_date;
                    _context.Add(schedule);

                    // add T_SCHEDULEPEOPLE, T_SCHEDULEGROUP
                    foreach (var item in request.MyStaffList)
                    {
                        var cd = Convert.ToInt32(item[2..]);
                        var type = item[..1];
                        if (type == "S")
                        {
                            var people = new T_SCHEDULEPEOPLE(schedule_no, cd);
                            people.create_user = user_id;
                            people.create_date = now;
                            people.update_user = user_id;
                            people.update_date = now;

                            _context.Add(people);
                        }
                        //else
                        //{
                        //    var group = new T_SCHEDULEGROUP(schedule_no, cd);
                        //    _context.Add(group);
                        //}
                    }

                    // add T_SCHEDULEPLACE
                    foreach (var item in request.MyPlaceList)
                    {
                        var place = new T_SCHEDULEPLACE(schedule_no, item);
                        place.create_user = user_id;
                        place.create_date = now;
                        place.update_user = user_id;
                        place.update_date = now;

                        _context.Add(place);
                    }

                    // add T_SCHEDULE_REPETITION
                    if (request.repeat_type != SCHEDULE_REPETITION.NONE)
                    {
                        var repetition = new T_SCHEDULE_REPETITION
                        {
                            schedule_no = schedule_no,
                            type = request.repeat_type
                        };
                        if (request.time_from != null)
                            repetition.time_from = request.time_from;
                        if (request.time_to != null)
                            repetition.time_to = request.time_to;
                        if (request.repeat_date_from != null)
                            repetition.date_from = DateTime.Parse(request.repeat_date_from);
                        if (request.repeat_date_to != null)
                            repetition.date_to = DateTime.Parse(request.repeat_date_to);
                        if (request.repeat_type != SCHEDULE_REPETITION.DAILY && request.repeat_type != SCHEDULE_REPETITION.DAILY_NO_HOLIDAY)
                        {
                            repetition.every_on = request.every_on;
                        }
                        repetition.create_user = user_id;
                        repetition.create_date = now;
                        repetition.update_user = user_id;
                        repetition.update_date = now;

                        _context.Add(repetition);
                    }

                    if (schedule_no_origin != 0) // in case of copy
                    {
                        var file_nos_origin = _context.T_SCHEDULE_FILE.Where(x => x.schedule_no == schedule_no_origin).Select(x => x.file_no).ToList();
                        if (file_nos_origin != null)
                        {
                            var file_nos_remove = new List<int>();
                            if (request.file_nos_remove != null)
                                file_nos_remove = request.file_nos_remove.Split(",").Select(x => Convert.ToInt32(x)).ToList();

                            if (file_nos_origin.Count > 0)
                            {
                                var files_copy = _context.T_SCHEDULE_FILE.Where(x => file_nos_origin.Contains(x.file_no) && !file_nos_remove.Contains(x.file_no) && x.schedule_no == schedule_no_origin)
                                    .Select(x => new T_SCHEDULE_FILE
                                    {
                                        schedule_no = schedule_no,
                                        file_no = x.file_no,
                                        filepath = x.filepath,
                                        filename = x.filename,
                                        create_user = user_id,
                                        create_date = now,
                                        update_user = user_id,
                                        update_date = now
                                    }).ToList();

                                _context.AddRange(files_copy);
                            }
                        }
                    }

                    AddFiles(request.work_dir, schedule_no);

                    await _context.SaveChangesAsync();
                    tran.Commit();

                    var dir = Path.Combine(_uploadPath, request.work_dir);
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();

                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }
            }

            return true;
        }

        protected ScheduleDetailViewModel? EditScheduleView(int schedule_no, string start_date)
        {
            ViewBag.StartDate = start_date;
            TempData["start_date"] = start_date;

            var viewModel = GetDetailView(schedule_no);
            if (viewModel == null)
            {
                return null;
            }

            var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
            string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
            string dir = Path.Combine(_uploadPath, dir_work);
            //workディレクトリの作成
            Directory.CreateDirectory(dir);
            viewModel.work_dir = dir_work;

            return viewModel;
        }

        [HttpPost]
        protected async Task<bool> UpdateSchedule(ScheduleDetailViewModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }

                // check if duplicated
                foreach (var item in request.MyPlaceList)
                {
                    if (HasAlreadyReservedPlace(item, request))
                    {
                        ModelState.AddModelError("", Messages.PLACE_ALREADY_RESERVED);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                        PrepareViewModel(request);
                        return false;
                    }
                }

                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }

                var list_allowed_file_extentions = new List<string>() { ".pdf" };
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (!list_allowed_file_extentions.Contains(Path.GetExtension(request.File[i].FileName).ToLower()))
                    {
                        ModelState.AddModelError("", Messages.BOARD_ALLOWED_FILE_EXTENSIONS);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                        PrepareViewModel(request);
                        return false;
                    }
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                        PrepareViewModel(request);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }

            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var user_id = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    var schedule_no = request.schedule_no;
                    var now = DateTime.Now;

                    // delete T_SCHEDULEPEOPLE, T_SCHEDULEGROUP, T_SCHEDULEPLACE
                    var people = _context.T_SCHEDULEPEOPLE.Where(x => x.schedule_no == schedule_no).ToList();
                    if (people != null)
                        _context.T_SCHEDULEPEOPLE.RemoveRange(people);

                    //var group = _context.T_SCHEDULEGROUP.Where(x => x.schedule_no == schedule_no).ToList();
                    //if (group != null)
                    //    _context.T_SCHEDULEGROUP.RemoveRange(group);

                    var places = _context.T_SCHEDULEPLACE.Where(x => x.schedule_no == schedule_no).ToList();
                    if (places != null)
                        _context.T_SCHEDULEPLACE.RemoveRange(places);

                    // delete T_SCHEDULE_REPETITION
                    var repetition = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == schedule_no);
                    if (repetition != null)
                        _context.T_SCHEDULE_REPETITION.Remove(repetition);

                    _context.SaveChanges();

                    // update T_SCHEDULE
                    var schedule = _context.T_SCHEDULE.FirstOrDefault(m => m.schedule_no == schedule_no);

                    schedule.schedule_type = request.schedule_type > 0 ? request.schedule_type : schedule.schedule_type;
                    if (request.repeat_type == SCHEDULE_REPETITION.NONE)
                    {
                        schedule.start_datetime = request.start_datetime != null ? DateTime.Parse(request.start_datetime) : schedule.start_datetime;
                        schedule.end_datetime = request.end_datetime != null ? DateTime.Parse(request.end_datetime) : schedule.end_datetime;
                    } else
                    {
                        schedule.start_datetime = null;
                        schedule.end_datetime = null;
                    }
                    schedule.title = !string.IsNullOrEmpty(request.title) ? request.title : schedule.title;
                    schedule.memo = !string.IsNullOrEmpty(request.memo) ? request.memo : schedule.memo;
                    schedule.is_private = request.is_private;

                    schedule.update_user = user_id;
                    schedule.update_date = now;

                    _context.Update(schedule);

                    // add T_SCHEDULEPEOPLE, T_SCHEDULEPLACE
                    foreach (var item in request.MyStaffList)
                    {
                        var cd = Convert.ToInt32(item[2..]);
                        var type = item[..1];
                        if (type == "S")
                        {
                            var people_ = new T_SCHEDULEPEOPLE(schedule_no, cd);
                            people_.create_user = user_id;
                            people_.create_date = now;
                            people_.update_user = user_id;
                            people_.update_date = now;

                            _context.Add(people_);
                        }
                        //else
                        //{
                        //    var group_ = new T_SCHEDULEGROUP(schedule_no, cd);
                        //    _context.Add(group_);
                        //}
                    }

                    foreach (var item in request.MyPlaceList)
                    {
                        var place = new T_SCHEDULEPLACE(schedule_no, item);
                        place.create_user = user_id;
                        place.create_date = now;
                        place.update_user = user_id;
                        place.update_date = now;

                        _context.Add(place);
                    }

                    // add T_SCHEDULE_REPETITION
                    if (request.repeat_type != SCHEDULE_REPETITION.NONE)
                    {
                        repetition = new T_SCHEDULE_REPETITION
                        {
                            schedule_no = schedule_no,
                            type = request.repeat_type
                        };
                        if (request.time_from != null)
                            repetition.time_from = request.time_from;
                        if (request.time_to != null)
                            repetition.time_to = request.time_to;
                        if (request.repeat_date_from != null)
                            repetition.date_from = DateTime.Parse(request.repeat_date_from);
                        if (request.repeat_date_to != null)
                            repetition.date_to = DateTime.Parse(request.repeat_date_to);
                        if (request.repeat_type != SCHEDULE_REPETITION.DAILY && request.repeat_type != SCHEDULE_REPETITION.DAILY_NO_HOLIDAY)
                        {
                            repetition.every_on = request.every_on;
                        }
                        repetition.create_user = user_id;
                        repetition.create_date = now;
                        repetition.update_user = user_id;
                        repetition.update_date = now;

                        _context.Add(repetition);
                    }

                    //レコード登録前にmainからファイル削除
                    if (request.Delete_files != null)
                    {
                        var arr_delete_files = request.Delete_files.Split(':');
                        string dir_main = Path.Combine(_uploadPath, schedule_no.ToString());
                        for (int i = 0; i < arr_delete_files.Length; i++)
                        {
                            if (arr_delete_files[i] != "")
                            {
                                var model_file = _context.T_SCHEDULE_FILE.First(x => x.schedule_no == request.schedule_no && x.filename == arr_delete_files[i]);
                                _context.T_SCHEDULE_FILE.Remove(model_file);

                                var filepath = Path.Combine(dir_main, arr_delete_files[i]);
                                System.IO.File.Delete(filepath);
                                System.IO.File.Delete(Path.ChangeExtension(filepath, "jpeg"));
                            }
                        }
                    }

                    AddFiles(request.work_dir, schedule_no);
                    
                    await _context.SaveChangesAsync();
                    tran.Commit();

                    var dir = Path.Combine(_uploadPath, request.work_dir);
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();

                    ModelState.AddModelError("", Message_change.FAILURE_001);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.SCHEDULE, request.work_dir);
                    PrepareViewModel(request);
                    return false;
                }
            }

            var start_date = TempData["start_date"] != null ? DateTime.Parse(TempData["start_date"].ToString()).ToString("yyyy-MM-dd")
                    : DateTime.Now.ToString("yyyy-MM-dd");
            return true;
        }

        [HttpPost]
        public async Task UpdateDuration(int schedule_no, string start, string end)
        {
            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var user_id = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                    var schedule = _context.T_SCHEDULE.FirstOrDefault(m => m.schedule_no == schedule_no);
                    schedule.start_datetime = DateTime.Parse(start);
                    schedule.end_datetime = DateTime.Parse(end);

                    schedule.update_user = user_id;
                    schedule.update_date = DateTime.Now;

                    _context.T_SCHEDULE.Update(schedule);

                    await _context.SaveChangesAsync();
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                }
            }
        }

        protected async Task<bool> DeleteSchedule(int schedule_no)
        {
            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    // delete T_SCHEDULE
                    var schedule = _context.T_SCHEDULE.FirstOrDefault(m => m.schedule_no == schedule_no);
                    _context.T_SCHEDULE.Remove(schedule);

                    // delete T_SCHEDULEPEOPLE, T_SCHEDULEGROUP, T_SCHEDULEPLACE
                    var people = _context.T_SCHEDULEPEOPLE.Where(x => x.schedule_no == schedule_no).ToList();
                    if (people != null)
                        _context.T_SCHEDULEPEOPLE.RemoveRange(people);

                    //var group = _context.T_SCHEDULEGROUP.Where(x => x.schedule_no == schedule_no).ToList();
                    //if (group != null)
                    //    _context.T_SCHEDULEGROUP.RemoveRange(group);

                    var places = _context.T_SCHEDULEPLACE.Where(x => x.schedule_no == schedule_no).ToList();
                    if (places != null)
                        _context.T_SCHEDULEPLACE.RemoveRange(places);

                    // delete T_SCHEDULE_REPETITION
                    var repetition = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == schedule_no);
                    if (repetition != null)
                        _context.T_SCHEDULE_REPETITION.Remove(repetition);

                    // delete files
                    var model_files = _context.T_SCHEDULE_FILE.Where(x => x.schedule_no == schedule_no).ToList();
                    if (model_files != null && model_files.Count > 0)
                    {
                        _context.T_SCHEDULE_FILE.RemoveRange(model_files);
                        string dir_main = Path.Combine(_uploadPath, schedule_no.ToString());
                        Directory.Delete(dir_main, true);
                    }

                    await _context.SaveChangesAsync();
                    tran.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                }
            }
            return false;
        }

        private ScheduleDetailViewModel? GetDetailView(int schedule_no)
        {
            try
            {
                var schedulePeople = (from p in _context.T_SCHEDULEPEOPLE
                                      where p.schedule_no == schedule_no
                                      select new { p.schedule_no, p.staf_cd });
                                    //  .Union(
                                    //from g in _context.T_SCHEDULEGROUP
                                    //join gs in _context.T_GROUPSTAFF on g.group_cd equals gs.group_cd
                                    //where g.schedule_no == schedule_no
                                    //select new { g.schedule_no, gs.staf_cd });

                var model = (from s in _context.T_SCHEDULE
                             join p in schedulePeople on s.schedule_no equals p.schedule_no
                             let r = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == s.schedule_no)
                             let create_date = s.create_date.ToString("yyyy-MM-dd H:m")
                             let create_staff = _context.M_STAFF.FirstOrDefault(x => x.staf_cd.ToString() == s.create_user)
                             let update_date = s.update_date.ToString("yyyy-MM-dd H:m")
                             let update_staff = _context.M_STAFF.FirstOrDefault(x => x.staf_cd.ToString() == s.update_user)
                             select new ScheduleDetailViewModel
                             {
                                 schedule_no = schedule_no,
                                 schedule_type = s.schedule_type,

                                 start_datetime = (s.start_datetime != null) ? s.start_datetime.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                                 end_datetime = (s.end_datetime != null) ? s.end_datetime.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                                 title = s.title ?? "",
                                 memo = s.memo ?? "",
                                 is_private = s.is_private,
                                 repeat_type = r == null ? (byte)0 : r.type,
                                 every_on = r == null ? 0 : r.every_on,
                                 time_from = r == null ? null : (r.time_from ?? null),
                                 time_to = r == null ? null : (r.time_to ?? null),
                                 repeat_date_from = r == null ? null : (r.date_from != null ? r.date_from.Value.ToString("yyyy-MM-dd") : null),
                                 repeat_date_to = r == null ? null : (r.date_to != null ? r.date_to.Value.ToString("yyyy-MM-dd") : null),

                                 create_user = create_staff.staf_name,
                                 create_date = create_date,
                                 update_user = update_staff.staf_name,
                                 update_date = update_date
                             }).FirstOrDefault();

                if (model != null)
                {
                    model.ScheduleTypeList = _context.M_SCHEDULE_TYPE
                    .Select(x => new ScheduleTypeModel
                    {
                        schedule_type = x.schedule_type,
                        schedule_typename = x.schedule_typename,
                        color = x.color
                    }).ToList();

                    model.StaffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(x => new StaffModel
                    {
                        staf_cd = x.staf_cd,
                        staf_name = x.staf_name
                    }).ToList();

                    model.GroupList = _context.M_GROUP.Select(x => new EmployeeGroupModel
                    {
                        group_cd = x.group_cd,
                        group_name = x.group_name,
                        staffs = _context.T_GROUPSTAFF.Where(y => y.group_cd == x.group_cd).Select(y => y.staf_cd).ToList()
                    }).ToList();

                    model.PlaceList = _context.M_PLACE
                    .OrderBy(x => x.sort)
                    .Select(x => new PlaceModel
                    {
                        place_cd = x.place_cd,
                        duplicate = x.duplicate,
                        place_name = x.place_name
                    }).ToList();

                    model.fileModel.fileList = _context.T_SCHEDULE_FILE.Where(x => x.schedule_no == schedule_no).ToList();

                    var myStaffList = _context.T_SCHEDULEPEOPLE.Where(x => x.schedule_no == schedule_no).ToList();
                    //var myGroupList = _context.T_SCHEDULEGROUP.Where(x => x.schedule_no == schedule_no).ToList();
                    var myPlaceList = _context.T_SCHEDULEPLACE.Where(x => x.schedule_no == schedule_no).ToList();
                    if (myStaffList != null && myStaffList.Count > 0)
                    {
                        int n = 0;
                        if (myStaffList != null)
                            n += myStaffList.Count;
                        //if (myGroupList != null)
                        //    n += myGroupList.Count;

                        model.MyStaffList = new string[n];
                        int i = 0;
                        if (myStaffList != null)
                        {
                            foreach (var staff in myStaffList)
                            {
                                model.MyStaffList[i++] = "S-" + staff.staf_cd;
                            }
                        }
                        //if (myGroupList != null)
                        //{
                        //    foreach (var group in myGroupList)
                        //    {
                        //        model.MyStaffList[i++] = "G-" + group.group_cd;
                        //    }
                        //}
                    }

                    if (myPlaceList != null && myPlaceList.Count > 0)
                    {
                        model.MyPlaceList = new int[myPlaceList.Count];
                        for (int i = 0; i < myPlaceList.Count; i++)
                        {
                            model.MyPlaceList[i] = myPlaceList[i].place_cd;
                        }
                    }

                    return model;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
            }

            return null;
        }

        protected void PrepareViewModel(ScheduleDetailViewModel model)
        {
            try
            {
                model.StaffList = _context.M_STAFF
                .Where(x => x.retired != 1)
                .Select(x => new StaffModel
                {
                    staf_cd = x.staf_cd,
                    staf_name = x.staf_name
                }).ToList();
                model.GroupList = _context.M_GROUP.Select(x => new EmployeeGroupModel
                {
                    group_cd = x.group_cd,
                    group_name = x.group_name,
                    staffs = _context.T_GROUPSTAFF.Where(y => y.group_cd == x.group_cd).Select(y => y.staf_cd).ToList()
                }).ToList();

                model.PlaceList = _context.M_PLACE
                .OrderBy(x => x.sort)
                .Select(x => new PlaceModel
                {
                    place_cd = x.place_cd,
                    duplicate = x.duplicate,
                    place_name = x.place_name
                }).ToList();

                model.ScheduleTypeList = _context.M_SCHEDULE_TYPE
                .Select(x => new ScheduleTypeModel
                {
                    schedule_type = x.schedule_type,
                    schedule_typename = x.schedule_typename,
                    color = x.color
                }).ToList();
                if (model.schedule_no > 0)
                {
                    // 編集のみ
                    model.fileModel.fileList = _context.T_SCHEDULE_FILE.Where(x => x.schedule_no == model.schedule_no).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        protected void AddFiles(string work_dir, int schedule_no)
        {
            try
            {
                //ディレクトリ設定
                string dir_main = Path.Combine(_uploadPath, schedule_no.ToString());
                if (!Directory.Exists(dir_main))
                {
                    Directory.CreateDirectory(dir_main);
                }
                //レコード登録　workディレクトリ
                string dir = Path.Combine(_uploadPath, work_dir);
                var work_dir_files = Directory.GetFiles(dir);
                for (int i = 0; i < work_dir_files.Length; i++)
                {
                    var renamed_file = "";
                    //同名ファイルが存在していたら名前変更
                    if (System.IO.File.Exists(Path.Combine(dir_main, Path.GetFileName(work_dir_files[i]))))
                    {
                        var count = 1;
                        while (true)
                        {
                            var arr_work = work_dir_files[i].Split(".");
                            var kandidat = "";
                            for (var w = 0; w < arr_work.Length - 1; w++)
                            {
                                kandidat = kandidat + arr_work[w] + ".";
                            }
                            kandidat = kandidat[..^1];
                            kandidat = kandidat + '（' + count + '）';
                            // ファイルの拡張子を取得
                            string fileExtention = Path.GetExtension(work_dir_files[i]);
                            kandidat += fileExtention;
                            if (!System.IO.File.Exists(kandidat))
                            {
                                renamed_file = Path.Combine(dir, kandidat);
                                break;
                            }
                            count++;
                        }
                    }
                    else
                    {
                        renamed_file = work_dir_files[i];
                    }

                    var file_name = Path.GetFileName(renamed_file);
                    var user_id = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    var now = DateTime.Now;

                    T_SCHEDULE_FILE record_file = new()
                    {
                        schedule_no = schedule_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        filepath = Path.Combine(dir_main, file_name),
                        filename = file_name,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.T_SCHEDULE_FILE.Add(record_file);

                    //ファイルをworkからmainにコピー
                    System.IO.File.Copy(work_dir_files[i], Path.Combine(dir_main, file_name));
                    pdfFileToImg(Path.Combine(dir_main, file_name));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw new Exception();
            }
        }

        private bool HasAlreadyReservedPlace(int place_cd, ScheduleDetailViewModel model)
        {
            var place = _context.M_PLACE.FirstOrDefault(x => x.place_cd == place_cd);
            if (place == null) return false;
            if (place.duplicate) return false;

            var scheduleList = (from s in _context.T_SCHEDULE
                                join l in _context.T_SCHEDULEPLACE on s.schedule_no equals l.schedule_no
                                let r = _context.T_SCHEDULE_REPETITION.FirstOrDefault(x => x.schedule_no == s.schedule_no)
                                where s.schedule_no != model.schedule_no && l.place_cd == place_cd
                                select new
                                {
                                    l.place_cd,
                                    s.schedule_no,
                                    start_datetime = (s.start_datetime != null) ? s.start_datetime.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                                    end_datetime = (s.end_datetime != null) ? s.end_datetime.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                                    s.schedule_type,
                                    repeat_type = r == null ? 0 : r.type,
                                    every_on = r == null ? 0 : r.every_on,
                                    time_from = r == null ? null : (r.time_from ?? null),
                                    time_to = r == null ? null : (r.time_to ?? null),
                                    repeat_date_from = r == null ? null : (r.date_from != null ? r.date_from.Value.ToString("yyyy-MM-dd") : null),
                                    repeat_date_to = r == null ? null : (r.date_to != null ? r.date_to.Value.ToString("yyyy-MM-dd") : null)
                                }).ToList();

            if (scheduleList.Count == 0) return false;

            var schedule_no = model.schedule_no;
            var repeat_type = model.repeat_type;
            var start_datetime = model.start_datetime == null ? default : DateTime.Parse(model.start_datetime);
            var end_datetime = model.end_datetime == null ? default : DateTime.Parse(model.end_datetime);
            var time_from = model.time_from == null ? start_datetime.TimeOfDay : TimeSpan.Parse(model.time_from);
            var time_to = model.time_to == null ? end_datetime.TimeOfDay : TimeSpan.Parse(model.time_to);
            DateTime? repeat_date_from = model.repeat_date_from == null ? null : DateTime.Parse(model.repeat_date_from);
            DateTime? repeat_date_to = model.repeat_date_to == null ? null : DateTime.Parse(model.repeat_date_to);
            DateTime? range_from = repeat_date_from == null ? null : new DateTime(repeat_date_from?.Year ?? 0, repeat_date_from?.Month ?? 0, repeat_date_from?.Day ?? 0, time_from.Hours, time_from.Minutes, 0);
            DateTime? range_to = repeat_date_to == null ? null : new DateTime(repeat_date_to?.Year ?? 0, repeat_date_to?.Month ?? 0, repeat_date_to?.Day ?? 0, time_to.Hours, time_to.Minutes, 0);
            var every_on = model.every_on ?? 0;

            if (repeat_type == 0) // none
            {
                var diffDays = GetDiffDays(start_datetime, end_datetime);
                var nSchedule = scheduleList.Count;
                for (var i = 0; i < nSchedule; i++)
                {
                    var item = scheduleList[i];
                    
                    if (item.repeat_type == 0) // none
                    {
                        if (IsOverlay(start_datetime, end_datetime, item.start_datetime, item.end_datetime)) return true;
                    }
                    else if (item.repeat_type == 1) // daily
                    {
                        if (IsOverlayNoneDaily(start_datetime, end_datetime, item.repeat_date_from, item.repeat_date_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 2) // daily no holiday
                    {
                        if (item.repeat_date_from == null && item.repeat_date_to == null)
                        {
                            if (IsOverlayNoneDailyNoHoliday(diffDays, start_datetime, end_datetime, time_from, time_to, item.time_from, item.time_to)) return true;
                        }
                        else
                        {
                            var item_repeat_date_from = DateTime.Parse(item.repeat_date_from);
                            var item_repeat_date_to = DateTime.Parse(item.repeat_date_to);

                            var item_range_from = new DateTime(item_repeat_date_from.Year, item_repeat_date_from.Month, item_repeat_date_from.Day, time_from.Hours, time_from.Minutes, 0);
                            var item_range_to = new DateTime(item_repeat_date_to.Year, item_repeat_date_to.Month, item_repeat_date_to.Day, time_to.Hours, time_to.Minutes, 0);

                            if (IsOutOfRange(start_datetime, end_datetime, item_range_from, item_range_to)) continue;

                            var range = GetOverlayRange(start_datetime, end_datetime, item_range_from, item_range_to);
                            var overlay_datetime_from = range[0];
                            var overlay_datetime_to = range[1];

                            var overlayDiffDays = GetDiffDays(overlay_datetime_from, overlay_datetime_to);
                            if (IsOverlayNoneDailyNoHoliday(overlayDiffDays, overlay_datetime_from, overlay_datetime_to, time_from, time_to, item.time_from, item.time_to)) return true;
                        }
                    }
                    else if (item.repeat_type == 3) // weekly
                    {
                        var item_every_on = item.every_on ?? 0;
                        var item_every_day_of_week = item_every_on == 7 ? DayOfWeek.Sunday : (DayOfWeek)item_every_on;
                        if (IsOverlayNoneWeekly(item_every_day_of_week, start_datetime, end_datetime, item.repeat_date_from, item.repeat_date_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 4) // monthly
                    {
                        var item_every_on = item.every_on ?? 0;
                        if (IsOverlayNoneMonthly(item_every_on, start_datetime, end_datetime, item.repeat_date_from, item.repeat_date_to, item.time_from, item.time_to)) return true;
                    }
                }

            }
            else if (repeat_type == 1) // daily
            {
                var nSchedule = scheduleList.Count;
                for (var i = 0; i < nSchedule; i++)
                {
                    var item = scheduleList[i];
                    

                    if (item.repeat_type == 0) // none
                    {
                        if (IsOverlayNoneDaily(item.start_datetime, item.end_datetime, repeat_date_from, repeat_date_to, time_from, time_to)) return true;
                    }
                    else // daily | daily no holiday | weekly | monthly
                    {
                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to))
                        {
                            return true;
                        }
                    }
                }

            }
            else if (repeat_type == 2) // daily no holiday
            {
                var nSchedule = scheduleList.Count;
                for (var i = 0; i < nSchedule; i++)
                {
                    var item = scheduleList[i];
                    
                    if (item.repeat_type == 0) // none
                    {
                        var item_start_datetime = DateTime.Parse(item.start_datetime);
                        var item_end_datetime = DateTime.Parse(item.end_datetime);
                        var item_diffDays = GetDiffDays(item_start_datetime, item_end_datetime);

                        if (repeat_date_from == null && repeat_date_to == null)
                        {
                            if (IsOverlayNoneDailyNoHoliday(item_diffDays, item_start_datetime, item_end_datetime, item_start_datetime.TimeOfDay, item_end_datetime.TimeOfDay, time_from, time_to)) return true;
                        }
                        else
                        {
                            if (IsOutOfRange(item_start_datetime, item_end_datetime, range_from, range_to)) continue;

                            var range = GetOverlayRange(range_from, range_to, item_start_datetime, item_end_datetime);
                            var overlay_datetime_from = range[0];
                            var overlay_datetime_to = range[1];

                            var overlayDiffDays = GetDiffDays(overlay_datetime_from, overlay_datetime_to);
                            if (IsOverlayNoneDailyNoHoliday(overlayDiffDays, overlay_datetime_from, overlay_datetime_to, item_start_datetime.TimeOfDay, item_end_datetime.TimeOfDay, time_from, time_to)) return true;
                        }
                    }
                    else if (item.repeat_type == 1 || item.repeat_type == 2) // daily | daily no holiday
                    {
                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 3) // weekly
                    {
                        // item day of week is saturday or sunday
                        if (item.every_on == 6 || item.every_on == 7) continue;

                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 4) // monthly
                    {
                        var item_every_on = item.every_on ?? 0;
                        if (IsOverlayDailyNoHolidayMonthly(item_every_on, repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to, time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                }

            }
            else if (repeat_type == 3) // weekly
            {
                var nSchedule = scheduleList.Count;
                for (var i = 0; i < nSchedule; i++)
                {
                    var item = scheduleList[i];
                    
                    if (item.repeat_type == 0) // none
                    {
                        var every_day_of_week = every_on == 7 ? DayOfWeek.Sunday : (DayOfWeek)every_on;
                        if (IsOverlayNoneWeekly(every_day_of_week, item.start_datetime, item.end_datetime, repeat_date_from, repeat_date_to, time_from, time_to)) return true;
                    }
                    else if (item.repeat_type == 1) // daily
                    {
                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 2) // daily no holiday
                    {
                        // day of week is saturday or sunday
                        if (every_on == 6 || every_on == 7) continue;

                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 3) // weekly
                    {
                        if (every_on != item.every_on) continue;

                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 4) // monthly
                    {
                        var every_day_of_week = every_on == 7 ? DayOfWeek.Sunday : (DayOfWeek)every_on;
                        if (IsOverlayWeeklyMonthly(every_day_of_week, item.every_on ?? 0, repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to,
                            time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                }

            }
            else if (repeat_type == 4) // monthly
            {
                var nSchedule = scheduleList.Count;
                for (var i = 0; i < nSchedule; i++)
                {
                    var item = scheduleList[i];
                    
                    if (item.repeat_type == 0) // none
                    {
                        if (IsOverlayNoneMonthly(every_on, item.start_datetime, item.end_datetime, repeat_date_from, repeat_date_to, time_from, time_to)) return true;
                    }
                    else if (item.repeat_type == 1) // daily
                    {
                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                    else if (item.repeat_type == 2) // daily no holiday
                    {
                        if (IsOverlayDailyNoHolidayMonthly(item.every_on ?? 0, item.repeat_date_from, item.repeat_date_to, repeat_date_from, repeat_date_to, item.time_from, item.time_to, time_from, time_to)) return true;
                    }
                    else if (item.repeat_type == 3) // weekly
                    {
                        var item_every_on = item.every_on ?? 0;
                        var item_every_day_of_week = item_every_on == 7 ? DayOfWeek.Sunday : (DayOfWeek)item_every_on;
                        if (IsOverlayWeeklyMonthly(item_every_day_of_week, every_on, item.repeat_date_from, item.repeat_date_to, repeat_date_from, repeat_date_to,
                            item.time_from, item.time_to, time_from, time_to)) return true;
                    }
                    else if (item.repeat_type == 4) // monthly
                    {
                        if (every_on != item.every_on) continue;

                        if (IsOutOfRange(repeat_date_from, repeat_date_to, item.repeat_date_from, item.repeat_date_to)) continue;

                        if (IsOverlay(time_from, time_to, item.time_from, item.time_to)) return true;
                    }
                }
            }

            return false;
        }

        private static bool IsOverlayNoneDaily(DateTime datetimeFrom, DateTime datetimeTo, string? rangeFrom, string? rangeTo, string timeFrom, string timeTo)
        {
            return IsOverlayNoneDaily(datetimeFrom, datetimeTo,
                rangeFrom == null ? null : DateTime.Parse(rangeFrom),
                rangeTo == null ? null : DateTime.Parse(rangeTo),
                TimeSpan.Parse(timeFrom), TimeSpan.Parse(timeTo)
                );
        }

        private static bool IsOverlayNoneDaily(string datetimeFrom, string datetimeTo, DateTime? rangeFrom, DateTime? rangeTo, TimeSpan timeFrom, TimeSpan timeTo)
        {
            return IsOverlayNoneDaily(DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo), rangeFrom, rangeTo, timeFrom, timeTo);
        }

        private static bool IsOverlayNoneDaily(DateTime datetimeFrom, DateTime datetimeTo, DateTime? rangeFrom, DateTime? rangeTo, TimeSpan timeFrom, TimeSpan timeTo)
        {
            var timeFromA = datetimeFrom.TimeOfDay;
            var timeToA = datetimeTo.TimeOfDay;
            if (rangeFrom == null || rangeTo == null)
            {
                var diffDays = GetDiffDays(datetimeFrom, datetimeTo);
                if (diffDays == 0)
                {
                    if (IsOverlay(timeFromA, timeToA, timeFrom, timeTo)) return true;
                } else if (diffDays == 1)
                {
                    if (IsOverlayInverse(timeFromA, timeToA, timeFrom, timeTo)) return true;
                } else
                {
                    return true;
                }
            } else
            {
                var range_from = new DateTime(rangeFrom?.Year ?? 0, rangeFrom?.Month ?? 0, rangeFrom?.Day ?? 0, timeFrom.Hours, timeFrom.Minutes, 0);
                var range_to = new DateTime(rangeTo?.Year ?? 0, rangeTo?.Month ?? 0, rangeTo?.Day ?? 0, timeTo.Hours, timeTo.Minutes, 0);

                if (IsOutOfRange(datetimeFrom, datetimeTo, range_from, range_to)) return false;

                var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
                var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);
                DateTime fromA, toA;
                bool isFromTimeSpan, isToTimeSpan;

                if (from > rangeFrom)
                {
                    fromA = datetimeFrom;
                    isFromTimeSpan = true;
                } else
                {
                    fromA = rangeFrom ?? default;
                    isFromTimeSpan = false;
                }

                if (to < rangeTo)
                {
                    toA = datetimeTo;
                    isToTimeSpan = true;
                } else
                {
                    toA = rangeTo ?? default;
                    isToTimeSpan = false;
                }

                if (!isFromTimeSpan && !isToTimeSpan) return true;

                var diffDays = GetDiffDays(fromA, toA);

                if (!isFromTimeSpan && isToTimeSpan)
                {
                    if (diffDays == 0)
                    {
                        if (timeToA >= timeFrom) return true;
                    } else
                    {
                        return true;
                    }
                } else if (isFromTimeSpan && !isToTimeSpan)
                {
                    if (diffDays == 0)
                    {
                        if (timeFromA <= timeTo) return true;
                    }
                    else
                    {
                        return true;
                    }
                } else
                {
                    if (diffDays == 0)
                    {
                        if (IsOverlay(timeFromA, timeToA, timeFrom, timeTo)) return true;
                    }
                    else if (diffDays == 1)
                    {
                        if (IsOverlayInverse(timeFromA, timeToA, timeFrom, timeTo)) return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsOverlayNoneDailyNoHoliday(int diffDays, DateTime? from, DateTime? to,
                                                    string time_from, string time_to, TimeSpan item_time_from, TimeSpan item_time_to)
        {
            return IsOverlayNoneDailyNoHoliday(diffDays, from, to, TimeSpan.Parse(time_from), TimeSpan.Parse(time_to), item_time_from, item_time_to);
        }

        private static bool IsOverlayNoneDailyNoHoliday(int diffDays, DateTime? from, DateTime? to,
                                                    TimeSpan time_from, TimeSpan time_to, string item_time_from, string item_time_to)
        {
            return IsOverlayNoneDailyNoHoliday(diffDays, from, to, time_from, time_to, TimeSpan.Parse(item_time_from), TimeSpan.Parse(item_time_to));
        }

        private static bool IsOverlayNoneDailyNoHoliday(int diffDays, DateTime? from, DateTime? to,
                                                    TimeSpan time_from, TimeSpan time_to, TimeSpan item_time_from, TimeSpan item_time_to)
        {
            if (diffDays == 0)
            {
                // if selected day is saturday or sunday then no overlay, so continue
                if (from?.DayOfWeek == DayOfWeek.Sunday || from?.DayOfWeek == DayOfWeek.Saturday) return false;

                if (IsOverlay(time_from, time_to, item_time_from, item_time_to))
                {
                    return true;
                }

            }
            else if (diffDays == 1)
            {
                /*
                    * item     |   Monday   |   Tuesday   |   Wednesday   |   Thursday   |   Friday   |   Saturday   |   Sunday   |   next Monday   |
                    * case(4)          <--------->
                    * case(2)                                                                    <--------->
                    * case(1)                                                                                  <--------->
                    * case(3)                                                                                                 <--------->
                    */
                if (from?.DayOfWeek == DayOfWeek.Saturday) // case(1) selected date range is between (saturday and sunday)
                {
                    return false;

                }
                else if (from?.DayOfWeek == DayOfWeek.Friday) // case(2) selected date range is between (friday and saturday)
                {
                    if (item_time_to >= time_from)
                    {
                        return true;
                    }

                }
                else if (from?.DayOfWeek == DayOfWeek.Sunday) // case(3) selected date range is between (sunday and next monday)
                {
                    if (item_time_from <= time_to)
                    {
                        return true;
                    }

                }
                else // case(4) selected date range is between
                        // (monday and tuesday) | (tuesday and wednesday) | (wednesday and thursday) | (thursday and friday)
                {
                    if (item_time_from >= time_from || item_time_to <= time_to)
                    {
                        return true;
                    }
                }

            }
            else if (diffDays == 2)
            {
                /*
                    * item     |   Monday   |   Tuesday   |   Wednesday   |   Thursday   |   Friday   |   Saturday   |   Sunday   |   next Monday   |   next Tuesday   |
                    * case(3)          <------------------------>
                    *                                 <------------------------>
                    *                                                <------------------------>
                    *                                                                <--------------------->
                    *                                                                                                         <-------------------------->
                    * case(1)                                                                     <----------------------->
                    * case(2)                                                                                    <--------------------->
                    */
                if (from?.DayOfWeek == DayOfWeek.Friday) // case(1) selected date range is between (friday and sunday)
                {
                    if (item_time_to >= time_from)
                    {
                        return true;
                    }

                }
                else if (from?.DayOfWeek == DayOfWeek.Saturday) // case(2) selected date range is between (saturday and next monday)
                {
                    if (item_time_from <= time_to)
                    {
                        return true;
                    }

                }
                else // case(3) selected date range is between
                        // (monday and wednesday) | (tuesday and thursday) | (wednesday and friday) | (thursday and saturday) | (sunday and next tuesday)
                {
                    return true;
                }

            }
            else if (diffDays == 3)
            {
                /*
                    * item     |   Monday   |   Tuesday   |   Wednesday   |   Thursday   |   Friday   |   Saturday   |   Sunday   |   next Monday   |   next Tuesday   |
                    *                                                                             <------------------------------------->
                    */
                if (from?.DayOfWeek == DayOfWeek.Friday && to?.DayOfWeek == DayOfWeek.Monday) // friday - next monday
                {
                    if (time_from <= item_time_to || time_to >= item_time_from)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        private static bool IsOverlayNoneWeekly(DayOfWeek every_on, DateTime datetimeFrom, DateTime datetimeTo, string? rangeFrom, string? rangeTo, string timeFrom, string timeTo)
        {
            return IsOverlayNoneWeekly(every_on, datetimeFrom, datetimeTo,
                rangeFrom == null ? null : DateTime.Parse(rangeFrom),
                rangeTo == null ? null : DateTime.Parse(rangeTo),
                TimeSpan.Parse(timeFrom), TimeSpan.Parse(timeTo)
                );
        }

        private static bool IsOverlayNoneWeekly(DayOfWeek every_on, string datetimeFrom, string datetimeTo, DateTime? rangeFrom, DateTime? rangeTo, TimeSpan timeFrom, TimeSpan timeTo)
        {
            return IsOverlayNoneWeekly(every_on, DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo), rangeFrom, rangeTo, timeFrom, timeTo);
        }

        private static bool IsOverlayNoneWeekly(DayOfWeek every_on, DateTime datetimeFrom, DateTime datetimeTo, DateTime? rangeFrom, DateTime? rangeTo, TimeSpan timeFrom, TimeSpan timeTo)
        {
            var range_from = new DateTime(rangeFrom?.Year ?? 0, rangeFrom?.Month ?? 0, rangeFrom?.Day ?? 0, timeFrom.Hours, timeFrom.Minutes, 0);
            var range_to = new DateTime(rangeTo?.Year ?? 0, rangeTo?.Month ?? 0, rangeTo?.Day ?? 0, timeTo.Hours, timeTo.Minutes, 0);

            if (IsOutOfRange(datetimeFrom, datetimeTo, range_from, range_to)) return false;

            var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
            var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);
            int s = 0, e = 0;
            DateTime fromA = default, toA = default;
            bool isFromTimeSpan = false, isToTimeSpan = false;

            do
            {
                if (from.DayOfWeek == every_on)
                {
                    if (from >= rangeFrom)
                    {
                        if (s == 0)
                        {
                            fromA = datetimeFrom;
                            isFromTimeSpan = true;
                        }
                        else
                        {
                            fromA = from;
                            isFromTimeSpan = false;
                        }
                        break;
                    }
                }
                from = from.AddDays(1);
                s++;
            } while (from <= to);

            do
            {
                if (to.DayOfWeek == every_on)
                {
                    if (to <= rangeTo)
                    {
                        if (e == 0)
                        {
                            toA = datetimeTo;
                            isToTimeSpan = true;
                        }
                        else
                        { 
                            toA = to;
                            isToTimeSpan = false;
                        }
                        break;
                    }
                }
                to = to.AddDays(-1);
                e++;
            } while (to >= from);

            if (fromA == default || toA == default) return false;

            if (!isFromTimeSpan && !isToTimeSpan) return true;

            var diffDays = GetDiffDays(fromA, toA);

            var timeFromA = datetimeFrom.TimeOfDay;
            var timeToA = datetimeTo.TimeOfDay;

            if (!isFromTimeSpan && isToTimeSpan)
            {
                if (diffDays == 0)
                {
                    if (timeToA >= timeFrom) return true;
                }
                else
                {
                    return true;
                }
            }
            else if (isFromTimeSpan && !isToTimeSpan)
            {
                if (diffDays == 0)
                {
                    if (timeFromA <= timeTo) return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (diffDays == 0)
                {
                    if (timeFromA < timeTo) return true;
                }
                else if (diffDays == 1)
                {
                    if (IsOverlayInverse(timeFromA, timeToA, timeFrom, timeTo)) return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOverlayNoneMonthly(int every_on, DateTime datetimeFrom, DateTime datetimeTo, string? rangeFrom, string? rangeTo, string timeFrom, string timeTo)
        {
            return IsOverlayNoneMonthly(every_on, datetimeFrom, datetimeTo,
                rangeFrom == null ? null : DateTime.Parse(rangeFrom),
                rangeTo == null ? null : DateTime.Parse(rangeTo),
                TimeSpan.Parse(timeFrom), TimeSpan.Parse(timeTo)
                );
        }

        private static bool IsOverlayNoneMonthly(int every_on, string datetimeFrom, string datetimeTo, DateTime? rangeFrom, DateTime? rangeTo, TimeSpan timeFrom, TimeSpan timeTo)
        {
            return IsOverlayNoneMonthly(every_on, DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo), rangeFrom, rangeTo, timeFrom, timeTo);
        }

        private static bool IsOverlayNoneMonthly(int every_on, DateTime datetimeFrom, DateTime datetimeTo, DateTime? rangeFrom, DateTime? rangeTo, TimeSpan timeFrom, TimeSpan timeTo)
        {
            var range_from = new DateTime(rangeFrom?.Year ?? 0, rangeFrom?.Month ?? 0, rangeFrom?.Day ?? 0, timeFrom.Hours, timeFrom.Minutes, 0);
            var range_to = new DateTime(rangeTo?.Year ?? 0, rangeTo?.Month ?? 0, rangeTo?.Day ?? 0, timeTo.Hours, timeTo.Minutes, 0);

            if (IsOutOfRange(datetimeFrom, datetimeTo, range_from, range_to)) return false;

            var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
            var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);
            int s = 0, e = 0;
            DateTime fromA = default, toA = default;
            bool isFromTimeSpan = false, isToTimeSpan = false;

            do
            {
                if (from.Day == every_on)
                {
                    if (from >= rangeFrom)
                    {
                        if (s == 0)
                        {
                            fromA = datetimeFrom;
                            isFromTimeSpan = true;
                        }
                        else
                        {
                            fromA = from;
                            isFromTimeSpan = false;
                        }
                        break;
                    }
                }
                from = from.AddDays(1);
                s++;
            } while (from <= to);

            do
            {
                if (to.Day == every_on)
                {
                    if (to <= rangeTo)
                    {
                        if (e == 0)
                        {
                            toA = datetimeTo;
                            isToTimeSpan = true;
                        }
                        else
                        {
                            toA = to;
                            isToTimeSpan = false;
                        }
                        break;
                    }
                }
                to = to.AddDays(-1);
                e++;
            } while (to >= from);

            if (fromA == default || toA == default) return false;

            if (!isFromTimeSpan && !isToTimeSpan) return true;

            var diffDays = GetDiffDays(fromA, toA);

            var timeFromA = datetimeFrom.TimeOfDay;
            var timeToA = datetimeTo.TimeOfDay;

            if (!isFromTimeSpan && isToTimeSpan)
            {
                if (diffDays == 0)
                {
                    if (timeToA >= timeFrom) return true;
                }
                else
                {
                    return true;
                }
            }
            else if (isFromTimeSpan && !isToTimeSpan)
            {
                if (diffDays == 0)
                {
                    if (timeFromA <= timeTo) return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (diffDays == 0)
                {
                    if (timeFromA < timeTo) return true;
                }
                else if (diffDays == 1)
                {
                    if (IsOverlayInverse(timeFromA, timeToA, timeFrom, timeTo)) return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOverlayDailyNoHolidayMonthly(int every_on, string? rangeFromA, string? rangeToA, DateTime? rangeFromB, DateTime? rangeToB, string timeFromA, string timeToA, TimeSpan timeFromB, TimeSpan timeToB)
        {
            DateTime? fromA = rangeFromA == null ? null : DateTime.Parse(rangeFromA);
            DateTime? toA = rangeToA == null ? null : DateTime.Parse(rangeToA);
            return IsOverlayDailyNoHolidayMonthly(every_on,
                fromA, toA,
                rangeFromB, rangeToB,
                TimeSpan.Parse(timeFromA), TimeSpan.Parse(timeToA),
                timeFromB, timeToB);
        }

        private static bool IsOverlayDailyNoHolidayMonthly(int every_on, DateTime? rangeFromA, DateTime? rangeToA, string? rangeFromB, string? rangeToB, TimeSpan timeFromA, TimeSpan timeToA, string timeFromB, string timeToB)
        {
            DateTime? fromB = rangeFromB == null ? null : DateTime.Parse(rangeFromB);
            DateTime? toB = rangeToB == null ? null : DateTime.Parse(rangeToB);
            return IsOverlayDailyNoHolidayMonthly(every_on,
                rangeFromA, rangeToA,
                fromB, toB,
                timeFromA, timeToA,
                TimeSpan.Parse(timeFromB), TimeSpan.Parse(timeToB));
        }

        private static bool IsOverlayDailyNoHolidayMonthly(int every_on, DateTime? rangeFromA, DateTime? rangeToA, DateTime? rangeFromB, DateTime? rangeToB, TimeSpan timeFromA, TimeSpan timeToA, TimeSpan timeFromB, TimeSpan timeToB)
        {
            if (IsOutOfRange(rangeFromA, rangeToA, rangeFromB, rangeToB)) return false;

            /*
            *                                                |---------- repeat range -----------|
            *  ------------------------------------------------- item repeat range(infinite) --------------------------------------
            */
            if (rangeFromB == null && rangeToB == null)
            {
                if (IsOverlay(timeFromA, timeToA, timeFromB, timeToB)) return true;
            }
            /*
                *  ------------------------------------------------------ repeat range(infinite) --------------------------------------
                *                                                |---------- item repeat range -----------|
                *                                                
                *                                                |------------- repeat range -------------|
                *                                          |---------- item repeat range -----------|
                *                                          
                *                                          |------------- repeat range -------------|
                *                                                |---------- item repeat range -----------|
                *                                          
                *                                          |------------------- repeat range -----------------|
                *                                                |---------- item repeat range -----------|
                *                                                
                *                                                |------------- repeat range -------------|
                *                                          |---------------- item repeat range ---------------|
                */
            else
            {
                DateTime overlay_repeat_date_from;
                DateTime overlay_repeat_date_to;

                if (rangeFromA == null && rangeToA == null)
                {
                    overlay_repeat_date_from = rangeFromB ?? default;
                    overlay_repeat_date_to = rangeToB ?? default;
                }
                else
                {
                    var range = GetOverlayRange(rangeFromA, rangeToA, rangeFromB, rangeToB);
                    overlay_repeat_date_from = range[0];
                    overlay_repeat_date_to = range[1];
                }

                // check if monthly day of week is on between monday and friday in the range of overlay
                var from = overlay_repeat_date_from;
                var to = overlay_repeat_date_to;
                var date_from = from.AddDays(1 - from.Day);
                var date_to = to.AddDays(1 - to.Day).AddMonths(1);
                while (true)
                {
                    if (date_from > date_to)
                        break;

                    var date = date_from.AddDays(every_on - 1);

                    date_from = date_from.AddMonths(1);

                    /*
                        * for example
                        * item every on : 12th
                        * 
                        * repeat date range         ->   | 6/10 ---------------- | 7/1 ------------- | 8/1 ------------- | 9/1 ------------- | 10/1 ------------ 10/15 |
                        * item repeat date range    ->              | 6/15 ------------------------------------------------------------------------- 10/9 |
                        * overlay repeat date range ->              | ----------------------------------------------------------------------------------- |
                        * date                      ->        6/12(x)                    7/12                  8/12              9/12                       10/12
                        * date.DayOfWeek            ->                                 Monday(o)            Saturday(x)        Sunday(x)                   Friday(o)
                        */

                    if (date < overlay_repeat_date_from || date > overlay_repeat_date_to) continue;

                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

                    if (IsOverlay(timeFromA, timeToA, timeFromB, timeToB)) return true;
                }
            }

            return false;
        }

        private static bool IsOverlayWeeklyMonthly(DayOfWeek every_day_of_week, int every_on, string? rangeFromA, string? rangeToA, DateTime? rangeFromB, DateTime? rangeToB, string timeFromA, string timeToA, TimeSpan timeFromB, TimeSpan timeToB)
        {
            DateTime? fromA = rangeFromA == null ? null : DateTime.Parse(rangeFromA);
            DateTime? toA = rangeToA == null ? null : DateTime.Parse(rangeToA);
            return IsOverlayWeeklyMonthly(every_day_of_week, every_on,
                fromA, toA,
                rangeFromB, rangeToB,
                TimeSpan.Parse(timeFromA), TimeSpan.Parse(timeToA),
                timeFromB, timeToB);
        }

        private static bool IsOverlayWeeklyMonthly(DayOfWeek every_day_of_week, int every_on, DateTime? rangeFromA, DateTime? rangeToA, string? rangeFromB, string? rangeToB, TimeSpan timeFromA, TimeSpan timeToA, string timeFromB, string timeToB)
        {
            DateTime? fromB = rangeFromB == null ? null : DateTime.Parse(rangeFromB);
            DateTime? toB = rangeToB == null ? null : DateTime.Parse(rangeToB);
            return IsOverlayWeeklyMonthly(every_day_of_week, every_on,
                rangeFromA, rangeToA,
                fromB, toB,
                timeFromA, timeToA,
                TimeSpan.Parse(timeFromB), TimeSpan.Parse(timeToB));
        }

        private static bool IsOverlayWeeklyMonthly(DayOfWeek every_day_of_week, int every_on, DateTime? rangeFromA, DateTime? rangeToA, DateTime? rangeFromB, DateTime? rangeToB, TimeSpan timeFromA, TimeSpan timeToA, TimeSpan timeFromB, TimeSpan timeToB)
        {
            if (IsOutOfRange(rangeFromA, rangeToA, rangeFromB, rangeToB)) return false;

            /*
            *                                                |---------- repeat range -----------|
            *  ------------------------------------------------- item repeat range(infinite) --------------------------------------
            */
            if (rangeFromB == null && rangeToB == null)
            {
                if (IsOverlay(timeFromA, timeToA, timeFromB, timeToB)) return true;
            }

            /*
            *  ------------------------------------------------------ repeat range(infinite) --------------------------------------
            *                                                |---------- item repeat range -----------|
            *                                                
            *                                                |------------- repeat range -------------|
            *                                          |---------- item repeat range -----------|
            *                                          
            *                                          |------------- repeat range -------------|
            *                                                |---------- item repeat range -----------|
            *                                          
            *                                          |------------------- repeat range -----------------|
            *                                                |---------- item repeat range -----------|
            *                                                
            *                                                |------------- repeat range -------------|
            *                                          |---------------- item repeat range ---------------|
            */
            else
            {
                DateTime overlay_repeat_date_from;
                DateTime overlay_repeat_date_to;

                if (rangeFromB == null && rangeToB == null)
                {
                    overlay_repeat_date_from = rangeFromB ?? default;
                    overlay_repeat_date_to = rangeToB ?? default;
                }
                else
                {
                    var range = GetOverlayRange(rangeFromA, rangeToA, rangeFromB, rangeToB);
                    overlay_repeat_date_from = range[0];
                    overlay_repeat_date_to = range[1];
                }

                // check if monthly day of week is on between monday and friday in the range of overlay
                var from = overlay_repeat_date_from;
                var to = overlay_repeat_date_to;
                var date_from = from.AddDays(1 - from.Day);
                var date_to = to.AddDays(1 - to.Day).AddMonths(1);
                while (true)
                {
                    if (date_from > date_to)
                        break;

                    var item_date = date_from.AddDays(every_on - 1);

                    date_from = date_from.AddMonths(1);

                    /*
                        * for example
                        * item every on : Tuesday
                        * 
                        * repeat date range         ->   | 6/10 --------------------- | 7/1 ------------- | 8/1 ------------- | 9/1 ------------- | 10/1 ------------ 10/15 |
                        * item repeat date range    ->                   | 6/15 ------------------------------------------------------------------------- 10/9 |
                        * overlay repeat date range ->                   | ----------------------------------------------------------------------------------- |
                        * item date                 ->        Tuesday(x)                                                                                         Tuesday(x)
                        * item date.DayOfWeek       ->                                       Tuesday(o)            Saturday(x)        Tuesday(o)
                        */

                    if (item_date < overlay_repeat_date_from || item_date > overlay_repeat_date_to) continue;

                    if (item_date.DayOfWeek != every_day_of_week) continue;

                    if (IsOverlay(timeFromA, timeToA, timeFromB, timeToB)) return true;
                }
            }

            return false;
        }


        /*
        * no overlaid cases
        *                               |---------- from - to ----------|
        *     |-- item from - to --|
        *     
        *     
        *                               |---------- from - to ----------|
        *                                                                    |-- item from - to --|
        */

        private static bool IsOverlay(DateTime? from, DateTime? to, string item_from, string item_to)
        {
            return IsOverlay(from, to, DateTime.Parse(item_from), DateTime.Parse(item_to));
        }

        private static bool IsOverlay(string from, string to, DateTime? item_from, DateTime? item_to)
        {
            return IsOverlay(DateTime.Parse(from), DateTime.Parse(to), item_from, item_to);
        }

        private static bool IsOverlay(DateTime? from, DateTime? to, DateTime? item_from, DateTime? item_to)
        {
            return (item_from <= to && item_to >= from);
        }

        private static bool IsOverlay(TimeSpan? from, TimeSpan? to, string item_from, string item_to)
        {
            return IsOverlay(from, to, TimeSpan.Parse(item_from), TimeSpan.Parse(item_to));
        }

        private static bool IsOverlay(TimeSpan? from, TimeSpan? to, TimeSpan item_from, TimeSpan item_to)
        {
            return (item_from <= to && item_to >= from);
        }


        private static bool IsOverlayInverse(DateTime? from, DateTime? to, string item_from, string item_to)
        {
            return IsOverlayInverse(from, to, DateTime.Parse(item_from), DateTime.Parse(item_to));
        }

        private static bool IsOverlayInverse(string from, string to, DateTime? item_from, DateTime? item_to)
        {
            return IsOverlayInverse(DateTime.Parse(from), DateTime.Parse(to), item_from, item_to);
        }

        private static bool IsOverlayInverse(DateTime? from, DateTime? to, DateTime? item_from, DateTime? item_to)
        {
            return (from <= item_to || to >= item_from);
        }

        private static bool IsOverlayInverse(TimeSpan? from, TimeSpan? to, string item_from, string item_to)
        {
            return IsOverlayInverse(from, to, TimeSpan.Parse(item_from), TimeSpan.Parse(item_to));
        }

        private static bool IsOverlayInverse(TimeSpan? from, TimeSpan? to, TimeSpan item_from, TimeSpan item_to)
        {
            return (from <= item_to || to >= item_from);
        }

        private static bool IsOutOfRange(DateTime? datefromA, DateTime? datetoA, DateTime? datefromB, DateTime? datetoB)
        {
            if (datefromA != null && datetoA != null && datefromB != null && datetoB != null)
                return (datefromA > datetoB || datetoA < datefromB);
            return false;
        }

        private static bool IsOutOfRange(DateTime? datefromA, DateTime? datetoA, string? datefromB, string? datetoB)
        {
            return IsOutOfRange(datefromA, datetoA,
                datefromB == null ? null : DateTime.Parse(datefromB),
                datetoB == null ? null : DateTime.Parse(datetoB));
        }

        private static bool IsOutOfRangeA(DateTime datetimeFrom, DateTime datetimeTo, DateTime? dateFrom, DateTime? dateTo)
        {
            if (dateFrom != null && dateTo != null)
            {
                var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
                var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);

                return (from > dateTo || to < dateFrom);
            }
            return false;
        }

        private static bool IsOutOfRangeA(DateTime datetimeFrom, DateTime datetimeTo, string? dateFrom, string? dateTo)
        {
            return IsOutOfRangeA(datetimeFrom, datetimeTo,
                dateFrom == null ? null : DateTime.Parse(dateFrom),
                dateTo == null ? null : DateTime.Parse(dateTo));
        }

        private static bool IsOutOfRangeA(string? datetimeFrom, string? datetimeTo, DateTime? dateFrom, DateTime? dateTo)
        {
            return IsOutOfRangeA(DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo), dateFrom, dateTo);
        }

        private static bool IsOutOfRangeB(DateTime? dateFrom, DateTime? dateTo, DateTime datetimeFrom, DateTime datetimeTo)
        {
            if (dateFrom != null && dateTo != null)
            {
                var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
                var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);

                return (dateFrom > to || dateTo < from);
            }
            return false;
        }

        private static bool IsOutOfRangeB(DateTime? dateFrom, DateTime? dateTo, string datetimeFrom, string datetimeTo)
        {
            return IsOutOfRangeB(dateFrom, dateTo, DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo));
        }

        private static bool IsOutOfRangeB(string? dateFrom, string? dateTo, DateTime datetimeFrom, DateTime datetimeTo)
        {
            return IsOutOfRangeB(
                dateFrom == null ? null : DateTime.Parse(dateFrom),
                dateTo == null ? null : DateTime.Parse(dateTo),
                datetimeFrom, datetimeTo);
        }

        private static DateTime[] GetOverlayRange(DateTime? fromA, DateTime? toA, DateTime? fromB, DateTime? toB)
        {
            var overlay_datetime_from = fromB > fromA ? fromB : fromA;
            var overlay_datetime_to = toB < toA ? toB : toA;
            return new DateTime[2] { overlay_datetime_from ?? default, overlay_datetime_to ?? default };
        }

        private static DateTime[] GetOverlayRange(DateTime? fromA, DateTime? toA, string? fromB, string? toB)
        {
            var item_from_ = DateTime.Parse(fromB);
            var item_to_ = DateTime.Parse(toB);
            return GetOverlayRange(fromA, toA, item_from_, item_to_);
        }

        private static DateTime[] GetOverlayRangeA(DateTime datetimeFrom, DateTime datetimeTo, DateTime? dateFrom, DateTime? dateTo)
        {
            var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
            var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);

            var overlay_datetime_from = dateFrom > from ? dateFrom : from;
            var overlay_datetime_to = dateTo < to ? dateTo : to;
            return new DateTime[2] { overlay_datetime_from ?? default, overlay_datetime_to ?? default };
        }

        private static DateTime[] GetOverlayRangeA(DateTime datetimeFrom, DateTime datetimeTo, string? dateFrom, string? dateTo)
        {
            return GetOverlayRangeA(datetimeFrom, datetimeTo,
                dateFrom == null ? null : DateTime.Parse(dateFrom),
                dateTo == null ? null : DateTime.Parse(dateTo));
        }

        private static DateTime[] GetOverlayRangeA(string datetimeFrom, string datetimeTo, DateTime? dateFrom, DateTime? dateTo)
        {
            return GetOverlayRangeA(DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo), dateFrom, dateTo);
        }

        private static DateTime[] GetOverlayRangeB(DateTime? dateFrom, DateTime? dateTo, DateTime datetimeFrom, DateTime datetimeTo)
        {
            var from = new DateTime(datetimeFrom.Year, datetimeFrom.Month, datetimeFrom.Day);
            var to = new DateTime(datetimeTo.Year, datetimeTo.Month, datetimeTo.Day);

            var overlay_datetime_from = dateFrom > from ? dateFrom : from;
            var overlay_datetime_to = dateTo < to ? dateTo : to;
            return new DateTime[2] { overlay_datetime_from ?? default, overlay_datetime_to ?? default };
        }

        private static DateTime[] GetOverlayRangeB(DateTime? dateFrom, DateTime? dateTo, string datetimeFrom, string datetimeTo)
        {
            return GetOverlayRangeB(dateFrom, dateTo, DateTime.Parse(datetimeFrom), DateTime.Parse(datetimeTo));
        }

        private static DateTime[] GetOverlayRangeB(string? dateFrom, string? dateTo, DateTime datetimeFrom, DateTime datetimeTo)
        {
            return GetOverlayRangeB(dateFrom == null ? null : DateTime.Parse(dateFrom),
                dateTo == null ? null : DateTime.Parse(dateTo), dateFrom, dateTo);
        }

        private static int GetDiffDays(DateTime from, DateTime to)
        {
            return (new DateTime(to.Year, to.Month, to.Day) - new DateTime(from.Year, from.Month, from.Day)).Days;
        }
    }
}