using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using web_groupware.Utilities;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using DocumentFormat.OpenXml.Spreadsheet;
using Format = web_groupware.Utilities.Format;

namespace web_groupware.Controllers
{
    public class TodoController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        private readonly string _uploadPath;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public TodoController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
            var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.TODO);
            if (t_dic == null || t_dic.content == null)
            {
                _logger.LogError(Messages.ERROR_PREFIX + Messages.DICTIONARY_FILE_PATH_NO_EXIST, DIC_KB.SAVE_PATH_FILE, DIC_KB_700_DIRECTORY.TODO);
                throw new Exception(Messages.DICTIONARY_FILE_PATH_NO_EXIST);
            }
            else
            {
                _uploadPath = t_dic.content;
            }
            _environment = hostingEnvironment;
        }

        [Authorize]
        public async Task<IActionResult> Index(string? response_status = null, string? deadline_set = null, string? keyword = null)
        {
            try
            {
                var model = CreateViewModel(response_status, deadline_set, keyword);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }

        }

        [HttpPost]
        public IActionResult Index(TodoViewModel request)
        {
            try
            {
                var model = CreateViewModel(request.search_response_status, request.search_deadline_set, request.search_keyword);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private TodoViewModel CreateViewModel(string? search_response_status, string? search_deadline_set, string? search_keyword)
        {
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;

                var todoList = _context.T_TODO.Where(x => x.staf_cd == user_id).ToList();

                if (!search_response_status.IsNullOrEmpty() && search_response_status != "-1")
                {
                    todoList = todoList.Where(x => x.response_status == Convert.ToInt32(search_response_status)).ToList();
                }
                if (!search_deadline_set.IsNullOrEmpty() && search_deadline_set != "-1")
                {
                    todoList = todoList.Where(x => x.deadline_set == Convert.ToInt32(search_deadline_set)).ToList();
                }
                if (!search_keyword.IsNullOrEmpty())
                {
                    todoList = todoList.Where(x => x.title.Contains(search_keyword) || x.description.Contains(search_keyword)).ToList();
                }

                TodoViewModel model = new TodoViewModel();

                model.fileList.AddRange(todoList.Select(todo => new TodoModel
                {
                    todo_no = todo.todo_no,
                    sendUrl = todo.sendUrl,
                    title = todo.title,
                    description = todo.description,
                    response_status = todo.response_status,
                    deadline_set = todo.deadline_set,
                    public_set = todo.public_set,
                    staf_cd = todo.staf_cd,
                    deadline_date = todo.deadline_date?.ToString("yyyy/MM/dd"),
                    create_date = todo.create_date.ToString("yyyy年M月d日 H時m分"),
                    has_file = _context.T_TODO_FILE.Where(x => x.todo_no == todo.todo_no).ToList().Count()
                }));

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(Message_register.FAILURE_001);
                }

                var viewModel = new TodoDetailModel();
                PrepareViewModel(viewModel);

                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                viewModel.MyStaffList = new string[1];
                viewModel.MyStaffList[0] = "S-" + user_id;
                string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                string dir = Path.Combine(_uploadPath, dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(dir);
                viewModel.work_dir = dir_work;
                viewModel.fileModel.editable = 1;
                viewModel.Upload_file_allowed_extension_1 = UPLOAD_FILE_ALLOWED_EXTENSION.IMAGE_PDF;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(TodoDetailModel request)
        {
            try
            {
                ModelState.Clear();
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.deadline_set == 1 && request.deadline_date.IsNullOrEmpty())
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                    PrepareViewModel(request);

                    return View(request);
                }
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                        PrepareViewModel(request);
                        return View(request);
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
                    var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                    var now = DateTime.Now;
                    
                    var todo_no = GetNextNo(DataTypes.TODO_NO);

                    DateTime? deadlineDate = null;
                    if (!string.IsNullOrEmpty(request.deadline_date))
                    {
                        if (DateTime.TryParseExact(request.deadline_date, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                        {
                            deadlineDate = parsedDate;
                        }
                    }

                    var model = new T_TODO
                    {
                        todo_no = todo_no,
                        title = request.title,
                        description = request.description,
                        sendUrl = request.sendUrl,
                        public_set = request.public_set,
                        deadline_set = request.deadline_set,
                        response_status = request.response_status,
                        staf_cd = user_id,
                        deadline_date = deadlineDate,
                        update_user = user_id,
                        update_date = now,
                        create_date = now,
                        create_user = user_id,
                    };

                    _context.Add(model);

                    foreach (var item in request.MyStaffList)
                    {
                        var cd = Convert.ToInt32(item[2..]);
                        var type = item[..1];
                        if (type == "S")
                        {
                            var target = new T_TODOTARGET
                            {
                                todo_no = todo_no,
                                staf_cd = cd,
                                create_user = user_id,
                                create_date = now,
                                update_user = user_id,
                                update_date = now,
                            };
                            _context.Add(target);
                        }
                    }

                    AddFiles(request.work_dir, todo_no);

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
                    throw;
                }
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult DownloadFile(string fileName)
        {
            try {
                var filePath = Path.Combine(_environment.WebRootPath, "files", fileName);
                if (filePath == null) {
                    return BadRequest("File is not provided or empty.");
                }
                var fileStream = System.IO.File.OpenRead(filePath);

                return File(fileStream, "application/octet-stream", fileName);
            
            }

            catch(Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return StatusCode(500, "An error occurred while uploading the file.");
            }
        }

        [HttpGet]
        public IActionResult Update(int todo_no)
        {
            try
            {
                var viewModel = getTodoDetail(todo_no);
                PrepareViewModel(viewModel);
                if (viewModel == null)
                {
                    return (IActionResult)Index();
                }

                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                string dir = Path.Combine(_uploadPath, dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(dir);
                viewModel.work_dir = dir_work;
                viewModel.Upload_file_allowed_extension_1 = UPLOAD_FILE_ALLOWED_EXTENSION.IMAGE_PDF;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }


        public async Task<T_TODO> itemGet(int todo_no)
        {

            var item = await _context.T_TODO.FirstOrDefaultAsync(m => m.todo_no == todo_no);

            return item;
        }

        public TodoDetailModel? getTodoDetail(int todo_no)
        {
            var item = _context.T_TODO.FirstOrDefault(m => m.todo_no == todo_no);

            var model = new TodoDetailModel
            {
                todo_no = item.todo_no,
                title = item.title,
                description = item.description,
                public_set = item.public_set,
                deadline_set = item.deadline_set,
                response_status = item.response_status,
                deadline_date = item.deadline_date?.ToString("yyyy/MM/dd"),
                update_date = item.update_date.ToString("yyyy-MM-dd H:m"),
                update_user = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == Convert.ToInt32(item.update_user)).staf_name,
                create_date = item.create_date.ToString("yyyy-MM-dd H:m"),
                create_user = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == Convert.ToInt32(item.create_user)).staf_name
            };

            model.fileModel.fileList = _context.T_TODO_FILE.Where(x => x.todo_no == todo_no).ToList();

            var myStaffList = _context.T_TODOTARGET.Where(x => x.todo_no == todo_no).ToList();
            if (myStaffList != null && myStaffList.Count > 0)
            {
                int n = 0;
                if (myStaffList != null)
                    n += myStaffList.Count;

                model.MyStaffList = new string[n];
                int i = 0;
                if (myStaffList != null)
                {
                    foreach (var staff in myStaffList)
                    {
                        model.MyStaffList[i++] = "S-" + staff.staf_cd;
                    }
                }

            }

            return model;
        }

        private void PrepareViewModel(TodoDetailModel model)
        {
            try
            {
                if (model.todo_no > 0)
                {
                    // 編集のみ
                    model.fileModel.fileList = _context.T_TODO_FILE.Where(x => x.todo_no == model.todo_no).ToList();
                }
                model.StaffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new StaffModel
                    {
                        staf_cd = u.staf_cd,
                        staf_name = u.staf_name
                    })
                    .ToList();
                model.GroupList = _context.M_GROUP.Select(x => new EmployeeGroupModel
                {
                    group_cd = x.group_cd,
                    group_name = x.group_name,
                    staffs = _context.T_GROUPSTAFF.Where(y => y.group_cd == x.group_cd).Select(y => y.staf_cd).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(TodoDetailModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.deadline_set == 1 && request.deadline_date.IsNullOrEmpty())
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.TODO, request.work_dir);
                        PrepareViewModel(request);
                        return View(request);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var now = DateTime.Now;
                

                var model = _context.T_TODO.FirstOrDefault(x => x.todo_no == request.todo_no);
                model.title = request.title;
                model.description = request.description;
                model.public_set = request.public_set;
                model.deadline_set = request.deadline_set;
                model.update_date = now;
                model.response_status = request.response_status;
                model.update_user = _context.M_STAFF.FirstOrDefault(x => x.staf_name == request.update_user).staf_cd.ToString();
                if (request.deadline_set == 0)
                {
                    model.deadline_date = null;
                }
                else
                {
                    model.deadline_date = DateTime.ParseExact(request.deadline_date, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                }
                
                _context.T_TODO.Update(model);

                var targetModel = _context.T_TODOTARGET.Where(x => x.todo_no == request.todo_no).ToList();
                if (targetModel.Count > 0 && targetModel != null)
                {
                    _context.T_TODOTARGET.RemoveRange(targetModel);
                }

                foreach (var item in request.MyStaffList)
                {
                    var cd = Convert.ToInt32(item[2..]);
                    var type = item[..1];
                    if (type == "S")
                    {
                        var target = new T_TODOTARGET
                        {
                            todo_no = request.todo_no,
                            staf_cd = cd,
                            create_user = user_id,
                            create_date = now,
                            update_user = user_id,
                            update_date = now,
                        };
                        _context.Add(target);
                    }
                }

                //レコード登録前にmainからファイル削除
                if (request.Delete_files != null)
                {
                    var arr_delete_files = request.Delete_files.Split(':');
                    string dir_main = Path.Combine(_uploadPath, request.todo_no.ToString());
                    for (int i = 0; i < arr_delete_files.Length; i++)
                    {
                        if (arr_delete_files[i] != "")
                        {
                            var model_file = _context.T_TODO_FILE.First(x => x.todo_no == request.todo_no && x.filename == arr_delete_files[i]);
                            _context.T_TODO_FILE.Remove(model_file);

                            var filepath = Path.Combine(dir_main, arr_delete_files[i]);
                            System.IO.File.Delete(filepath);
                            System.IO.File.Delete(Path.ChangeExtension(filepath, "jpeg"));
                        }
                    }
                }

                AddFiles(request.work_dir, request.todo_no);

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
                throw;
            }
            return RedirectToAction("Index");
        }

        protected async void AddFiles(string work_dir, int todo_no)
        {
            try
            {
                //ディレクトリ設定
                string dir_main = Path.Combine(_uploadPath, todo_no.ToString());
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
                    var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                    var now = DateTime.Now;

                    T_TODO_FILE record_file = new()
                    {
                        todo_no = todo_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        //filepath = Path.Combine(dir_main, file_name),
                        filepath = Path.Combine(todo_no.ToString(), file_name),
                        filename = file_name,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now,
                    };
                    await _context.T_TODO_FILE.AddAsync(record_file);

                    //ファイルをworkからmainにコピー
                    System.IO.File.Copy(work_dir_files[i], Path.Combine(dir_main, file_name));
                    // pdfFileToImg(Path.Combine(dir_main, file_name));
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
        public IActionResult Delete(int todo_no)
        {
            try
            {
                var viewModel = getTodoDetail(todo_no);
                PrepareViewModel(viewModel);
                if (viewModel == null)
                {
                    return (IActionResult)Index();
                }

                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                string dir = Path.Combine(_uploadPath, dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(dir);
                viewModel.work_dir = dir_work;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(TodoDetailModel request)
        {
            try
            {
                var todoTarget = _context.T_TODOTARGET.Where(x => x.todo_no == request.todo_no).ToList();
                if (todoTarget.Count > 0 && todoTarget != null)
                {
                    _context.T_TODOTARGET.RemoveRange(todoTarget);
                }

                var todoFile = _context.T_TODO_FILE.Where(x => x.todo_no == request.todo_no).ToList();
                if (todoFile.Count > 0 && todoFile != null)
                {
                    _context.T_TODO_FILE.RemoveRange(todoFile);
                }

                var todo = _context.T_TODO.FirstOrDefault(x => x.todo_no == request.todo_no);
                if (todo != null)
                {
                    _context.T_TODO.Remove(todo);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
            }
            return RedirectToAction("Index");
        }
    }
}

