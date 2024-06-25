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

        protected ScheduleDetailViewModel CreateScheduleView(string start_date)
        {
            var viewModel = new ScheduleDetailViewModel();
            PrepareViewModel(viewModel);

            viewModel.start_datetime = DateTime.Now.ToString("yyyy-MM-dd");
            viewModel.end_datetime = DateTime.Now.ToString("yyyy-MM-dd");
            viewModel.is_private = false;

            var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
            viewModel.MyStaffList = new string[1];
            viewModel.MyStaffList[0] = "S-" + user_id;

            string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
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
                    schedule.create_date = DateTime.Now;
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
                                        filename = x.filename
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
                    schedule.update_date = DateTime.Now;

                    _context.Update(schedule);

                    // add T_SCHEDULEPEOPLE, T_SCHEDULEPLACE
                    foreach (var item in request.MyStaffList)
                    {
                        var cd = Convert.ToInt32(item[2..]);
                        var type = item[..1];
                        if (type == "S")
                        {
                            var people_ = new T_SCHEDULEPEOPLE(schedule_no, cd);
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

                    model.GroupList = _context.T_GROUPM.Select(x => new EmployeeGroupModel
                    {
                        group_cd = x.group_cd,
                        group_name = x.group_name,
                        staffs = _context.T_GROUPSTAFF.Where(y => y.group_cd == x.group_cd).Select(y => y.staf_cd).ToList()
                    }).ToList();

                    model.PlaceList = _context.T_PLACEM
                    .OrderBy(x => x.sort)
                    .Select(x => new PlaceModel
                    {
                        place_cd = x.place_cd,
                        //duplicate = x.duplicate,
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

        private void PrepareViewModel(ScheduleDetailViewModel model)
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
                model.GroupList = _context.T_GROUPM.Select(x => new EmployeeGroupModel
                {
                    group_cd = x.group_cd,
                    group_name = x.group_name,
                    staffs = _context.T_GROUPSTAFF.Where(y => y.group_cd == x.group_cd).Select(y => y.staf_cd).ToList()
                }).ToList();

                model.PlaceList = _context.T_PLACEM
                .OrderBy(x => x.sort)
                .Select(x => new PlaceModel
                {
                    place_cd = x.place_cd,
                    //duplicate = x.duplicate,
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

                    T_SCHEDULE_FILE record_file = new()
                    {
                        schedule_no = schedule_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        filepath = Path.Combine(dir_main, file_name),
                        filename = file_name
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
    }
}