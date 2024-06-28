using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
/*using System.Security.Claims;*/
using Microsoft.EntityFrameworkCore;
using web_groupware.Utilities;
using Azure.Core;
using DocumentFormat.OpenXml.Office2016.Excel;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618, CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class WorkFlowController : BaseController
    {
        private readonly IWebHostEnvironment _environment;
        private const int FILE_TYPE_FILE = 0;
        private readonly string _uploadPath;

        public WorkFlowController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
            var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.WORKFLOW);
            if (t_dic == null || t_dic.content == null)
            {
                _logger.LogError(Messages.ERROR_PREFIX + Messages.DICTIONARY_FILE_PATH_NO_EXIST, DIC_KB.SAVE_PATH_FILE, DIC_KB_700_DIRECTORY.WORKFLOW);
                throw new Exception(Messages.DICTIONARY_FILE_PATH_NO_EXIST);
            }
            else
            {
                _uploadPath = t_dic.content;
            }
            _environment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            WorkFlowViewModel model = new WorkFlowViewModel();
            var userId = @User.FindFirst(ClaimTypes.STAF_CD).Value;

            var user = _context.M_STAFF.FirstOrDefault(m => m.staf_cd == int.Parse(userId));
            var user_auth = _context.M_STAFF_AUTH.FirstOrDefault(m => m.staf_cd == int.Parse(userId));

            var workflow_auth = user_auth.workflow_auth;
            if (userId == null || _context.T_WORKFLOW == null)
            {
                return Problem(userId);
            }
            if (workflow_auth == 0)
            {
                var items = _context.T_WORKFLOW.Where(item => item.update_user == userId).ToList();
                foreach (var item in items)
                {
                    model.fileList.Add(new WorkFlowDetail
                    {
                        id = item.id,
                        filename = item.filename,
                        title = item.title,
                        description = item.description,
                        icon = item.icon,
                        size = item.size,
                        type = item.type,
                        update_date = item.update_date,
                        manager_status = item.manager_status,
                        approver_status = item.approver_status
                    });
                }
            }
            else if (workflow_auth == 1)
            {
                var items = _context.T_WORKFLOW.Where(item => item.manager_status != 0).ToList();
                foreach (var item in items)
                {
                    model.fileList.Add(new WorkFlowDetail
                    {
                        id = item.id,
                        filename = item.filename,
                        title = item.title,
                        description = item.description,
                        icon = item.icon,
                        size = item.size,
                        type = item.type,
                        update_date = item.update_date,
                        manager_status = item.manager_status,
                        approver_status = item.approver_status
                    });
                }
            }
            else
            {
                var items = _context.T_WORKFLOW.Where(item => item.manager_status == 2).ToList();
                foreach (var item in items)
                {
                    model.fileList.Add(new WorkFlowDetail
                    {
                        id = item.id,
                        filename = item.filename,
                        title = item.title,
                        description = item.description,
                        icon = item.icon,
                        size = item.size,
                        type = item.type,
                        update_date = item.update_date,
                        manager_status = item.manager_status,
                        approver_status = item.approver_status
                    });
                }
            }

            return View(model);
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

                var viewModel = new WorkFlowDetail();
                PrepareViewModel(viewModel);

                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                string dir = Path.Combine(_uploadPath, dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(dir);
                viewModel.work_dir = dir_work;
                viewModel.fileModel.editable = 1;

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkFlowDetail request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                    PrepareViewModel(request);

                    return View(request);
                }
                var list_allowed_file_extentions = new List<string>() { ".pdf" };
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (!list_allowed_file_extentions.Contains(Path.GetExtension(request.File[i].FileName).ToLower()))
                    {
                        ModelState.AddModelError("", Messages.BOARD_ALLOWED_FILE_EXTENSIONS);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                        PrepareViewModel(request);
                        return View(request);
                    }
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
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
                    var userName = _context.M_STAFF.FirstOrDefault(x => x.staf_cd.ToString() == user_id).staf_name;
                    var now = DateTime.Now;

                    var workflow_no = GetNextNo(DataTypes.WORKFLOW_NO);
                    var model = new T_WORKFLOW
                    {
                        id = workflow_no,
                        title = request.title,
                        description = request.description,
                        manager_status = 0,
                        approver_status = 0,
                        update_user = user_id,
                        update_date = now,
                        create_user = user_id,
                        create_date = now,
                    };

                    _context.Add(model);

                    AddFiles(request.work_dir, workflow_no);

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

        [HttpPost]
        public async Task<IActionResult> ApplyWorkFlow(int id, int manager_status, int approver_status, string comment)
        {
            if (id < 1 || _context.T_WORKFLOW == null)
            {
                return RedirectToAction("Index");
            }
            var workFlowDetail = await _context.T_WORKFLOW.FindAsync(id);
            if(workFlowDetail != null)
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        workFlowDetail.manager_status = manager_status;
                        workFlowDetail.approver_status = approver_status;
                        workFlowDetail.comment = comment;
                        if(approver_status == 1) { 
                            workFlowDetail.manager = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                        } else if(approver_status > 1)
                        {
                            workFlowDetail.approver = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                        }

                        _context.T_WORKFLOW.Update(workFlowDetail);
                        await _context.SaveChangesAsync();

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        _logger.LogError(ex.Message);
                        _logger.LogError(ex.StackTrace);
                        tran.Dispose();
                        throw;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        protected async void AddFiles(string work_dir, int workflow_no)
        {

            try
            {
                //ディレクトリ設定
                string dir_main = Path.Combine(_uploadPath, workflow_no.ToString());
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

                    T_WORKFLOW_FILE record_file = new()
                    {
                        workflow_no = workflow_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        //filepath = Path.Combine(dir_main, file_name),
                        filepath = Path.Combine(workflow_no.ToString(), file_name),
                        filename = file_name
                    };
                    await _context.T_WORKFLOW_FILE.AddAsync(record_file);

                    //ファイルをworkからmainにコピー
                    System.IO.File.Copy(work_dir_files[i], Path.Combine(dir_main, file_name));
                    pdfFileToImg(Path.Combine(dir_main, file_name));
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
        public IActionResult Update(int id)
        {
            try
            {
                var viewModel = GetSelectedData(id);
                if (viewModel == null)
                {
                    return Index();
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
        public async Task<IActionResult> Update(WorkFlowDetail request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                var list_allowed_file_extentions = new List<string>() { ".pdf" };
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (!list_allowed_file_extentions.Contains(Path.GetExtension(request.File[i].FileName).ToLower()))
                    {
                        ModelState.AddModelError("", Messages.BOARD_ALLOWED_FILE_EXTENSIONS);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                        PrepareViewModel(request);
                        return View(request);
                    }
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
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

                var model = _context.T_WORKFLOW.FirstOrDefault(x => x.id == request.id);
                model.title = request.title;
                model.description = request.description;
                model.update_date = now;

                _context.T_WORKFLOW.Update(model);

                //レコード登録前にmainからファイル削除
                if (request.Delete_files != null)
                {
                    var arr_delete_files = request.Delete_files.Split(':');
                    string dir_main = Path.Combine(_uploadPath, request.id.ToString());
                    for (int i = 0; i < arr_delete_files.Length; i++)
                    {
                        if (arr_delete_files[i] != "")
                        {
                            var model_file = _context.T_WORKFLOW_FILE.First(x => x.workflow_no == request.id && x.filename == arr_delete_files[i]);
                            _context.T_WORKFLOW_FILE.Remove(model_file);

                            var filepath = Path.Combine(dir_main, arr_delete_files[i]);
                            System.IO.File.Delete(filepath);
                            System.IO.File.Delete(Path.ChangeExtension(filepath, "jpeg"));
                        }
                    }
                }

                AddFiles(request.work_dir, request.id);

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
        public WorkFlowDetail? GetSelectedData(int id)
        {
            var item = _context.T_WORKFLOW.FirstOrDefault(m => m.id == id);
            var model = new WorkFlowDetail
            {
                id = item.id,
                title = item.title,
                description = item.description
            };

            model.fileModel.fileList = _context.T_WORKFLOW_FILE.Where(x => x.workflow_no == id).ToList();

            return model;
        }

        private void PrepareViewModel(WorkFlowDetail model)
        {
            try
            {
                if (model.id > 0)
                {
                    // 編集のみ
                    model.fileModel.fileList = _context.T_WORKFLOW_FILE.Where(x => x.workflow_no == model.id).ToList();
                }
                model.staffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new WorkFlowViewModelStaff
                    {
                        staff_cd = u.staf_cd,
                        staff_name = u.staf_name
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.T_WORKFLOW == null)
            {
                return RedirectToAction("Index");
            }
            try
            {
                var fileDetail = await _context.T_WORKFLOW.FindAsync(id);
                if (fileDetail != null)
                {
                    if (fileDetail.type == FILE_TYPE_FILE)
                    {
                        var path = Path.Combine(_environment.WebRootPath, "uploads", fileDetail.filename);
                        var fileDel = new FileInfo(path);
                        fileDel.Delete();
                    }
                    _context.T_WORKFLOW.Remove(fileDetail);
                    await _context.SaveChangesAsync();
                }
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
        public async Task<IActionResult> DownloadFile(int? id)
        {
            try
            {
                var fileDetail = await _context.T_WORKFLOW.FindAsync(id);
                if (fileDetail != null && fileDetail.type == FILE_TYPE_FILE)
                {
                    var path = Path.Combine(_environment.WebRootPath, "uploads", fileDetail.filename);
                    var content = await System.IO.File.ReadAllBytesAsync(path);
                    new FileExtensionContentTypeProvider()
                                    .TryGetContentType(fileDetail.filename, out string contentType);
                    if (contentType == null) contentType = System.Net.Mime.MediaTypeNames.Application.Octet;

                    return File(content, contentType, fileDetail.filename);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
            return BadRequest();            
        }
    }
}