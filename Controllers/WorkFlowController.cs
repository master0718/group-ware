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
using System.Reflection.Metadata;
using DocumentFormat.OpenXml.Spreadsheet;
using Format = web_groupware.Utilities.Format;

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
            var userId = @User.FindFirst(ClaimTypes.STAF_CD).Value;
            var user_auth = _context.M_STAFF_AUTH.FirstOrDefault(m => m.staf_cd == int.Parse(userId));

            var workflow_auth = user_auth.workflow_auth;
            if (userId == null || _context.T_WORKFLOW == null)
            {
                return Problem(userId);
            }

            try
            {
                var model = CreateViewModel(workflow_auth, int.Parse(userId));
                return View(model);
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

                var viewModel = new WorkFlowDetailViewModel();
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
        public async Task<IActionResult> Create(WorkFlowDetailViewModel request)
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
                    var now = DateTime.Now;

                    var workflow_no = GetNextNo(DataTypes.WORKFLOW_NO);
                    var model = new T_WORKFLOW
                    {
                        workflow_no = workflow_no,
                        title = request.title,
                        description = request.description,
                        request_type = request.request_type,
                        status = WorkflowApproveStatus.DRAFT,
                        requester_cd = int.Parse(user_id),
                        request_date = now,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };

                    var approval_model = new T_WORKFLOW_APPROVAL
                    {
                        workflow_no = workflow_no,
                        approver_cd = request.approver_cd,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };

                    _context.Add(model);
                    _context.Add(approval_model);

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

        [HttpGet]
        public IActionResult Update(int workflow_no)
        {
            try
            {
                var viewModel = GetDetailView(workflow_no);
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
        public async Task<IActionResult> Update(WorkFlowDetailViewModel request)
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

                var model = _context.T_WORKFLOW.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                if (model.status != WorkflowApproveStatus.DRAFT)
                {
                    ModelState.AddModelError("", Messages.REQUEST_WORKFLOW_EDIT_VIOLATION);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.WORKFLOW, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                model.title = request.title;
                model.description = request.description;
                model.request_type = request.request_type;
                model.update_user = user_id;
                model.update_date = now;

                _context.T_WORKFLOW.Update(model);

                //レコード登録前にmainからファイル削除
                if (request.Delete_files != null)
                {
                    var arr_delete_files = request.Delete_files.Split(':');
                    string dir_main = Path.Combine(_uploadPath, request.workflow_no.ToString());
                    for (int i = 0; i < arr_delete_files.Length; i++)
                    {
                        if (arr_delete_files[i] != "")
                        {
                            var model_file = _context.T_WORKFLOW_FILE.First(x => x.workflow_no == request.workflow_no && x.filename == arr_delete_files[i]);
                            _context.T_WORKFLOW_FILE.Remove(model_file);

                            var filepath = Path.Combine(dir_main, arr_delete_files[i]);
                            System.IO.File.Delete(filepath);
                            System.IO.File.Delete(Path.ChangeExtension(filepath, "jpeg"));
                        }
                    }
                }

                AddFiles(request.work_dir, request.workflow_no);

                var approvals = _context.T_WORKFLOW_APPROVAL.Where(x => x.workflow_no == request.workflow_no).ToList();
                if (approvals != null)
                    _context.T_WORKFLOW_APPROVAL.RemoveRange(approvals);

                var approval = new T_WORKFLOW_APPROVAL
                {
                    workflow_no = request.workflow_no,
                    approver_cd = request.approver_cd,
                    create_user = user_id,
                    create_date = now,
                    update_user = user_id,
                    update_date = now
                };
                _context.T_WORKFLOW_APPROVAL.Add(approval);

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

        [HttpGet]
        public IActionResult Delete(int workflow_no)
        {
            var viewModel = GetDetailView(workflow_no);
            if (viewModel == null)
            {
                return RedirectToAction("Index");
            }
            return View("Delete", viewModel);
        }

        [HttpPost]
        public IActionResult Delete(WorkFlowDetailViewModel request)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var model = _context.T_WORKFLOW.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                if (model != null)
                    _context.T_WORKFLOW.Remove(model);

                var model_files = _context.T_WORKFLOW_FILE.Where(x => x.workflow_no == request.workflow_no).ToList();
                if (model_files != null && model_files.Count > 0)
                {
                    _context.T_WORKFLOW_FILE.RemoveRange(model_files);
                    string dir_main = Path.Combine(_uploadPath, request.workflow_no.ToString());
                    Directory.Delete(dir_main, true);
                }

                var approvals = _context.T_WORKFLOW_APPROVAL.Where(x => x.workflow_no == request.workflow_no).ToList();
                if (approvals != null)
                    _context.T_WORKFLOW_APPROVAL.RemoveRange(approvals);

                _context.SaveChanges();
                tran.Commit();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                tran.Dispose();
                ModelState.AddModelError("", Message_change.FAILURE_001);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Proposal(int workflow_no)
        {
            var viewModel = GetDetailView(workflow_no);
            if (viewModel == null)
            {
                return RedirectToAction("Index");
            }
            return View("Proposal", viewModel);
        }

        [HttpPost]
        public IActionResult Proposal(WorkFlowDetailViewModel request)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var now = DateTime.Now;

                var model = _context.T_WORKFLOW.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                model.status = WorkflowApproveStatus.REQUEST;
                model.request_date = now;
                model.update_date = now;
                _context.T_WORKFLOW.Update(model);

                var approval = _context.T_WORKFLOW_APPROVAL.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                if (approval != null)
                {
                    approval.approve_result = WorkflowApproveResult.NONE;
                    _context.T_WORKFLOW_APPROVAL.Update(approval);
                }

                var recipient = (from a in _context.T_WORKFLOW_APPROVAL
                                  join u in _context.M_STAFF on a.approver_cd equals u.staf_cd
                                  where a.workflow_no == model.workflow_no
                                  select u).ToList();

                //var host = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.MAIL_FROM_URL && x.dic_cd == "1")?.content;
                //var mailno_item = _context.T_SHARENO.FirstOrDefault(x => x.data_type == DataTypes.SENDMAIL_NO);
                //mail_transaction_no = 0;
                // // send notification mail

                _context.SaveChanges();
                tran.Commit();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                tran.Dispose();
                ModelState.AddModelError("", Message_change.FAILURE_001);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Accept(int workflow_no)
        {
            var viewModel = GetDetailView(workflow_no);
            if (viewModel == null)
            {
                return RedirectToAction("Index");
            }
            return View("Approve", viewModel);
        }

        [HttpPost]
        public IActionResult Accept(WorkFlowDetailViewModel request)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var model = _context.T_WORKFLOW.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                if (model.status == WorkflowApproveStatus.FINISH)
                {
                    ModelState.AddModelError("", Messages.WORKFLOW_APPROVAL_ALREADY_FINISHED);
                    PrepareViewModel(request);
                    return View(request);
                } else if (model.status == WorkflowApproveStatus.REJECT)
                {
                    ModelState.AddModelError("", Messages.WORKFLOW_APPROVAL_ALREADY_REJECTED);
                    PrepareViewModel(request);
                    return View(request);
                }

                var approval = _context.T_WORKFLOW_APPROVAL.FirstOrDefault(x => x.workflow_no == request.workflow_no);

                if (request.is_accept == 0)
                {
                    model.status = WorkflowApproveStatus.REJECT;
                    approval.approve_result = WorkflowApproveResult.REJECT;
                } else
                {
                    model.status = WorkflowApproveStatus.FINISH;
                    approval.approve_result = WorkflowApproveResult.ACCEPT;
                }

                var now = DateTime.Now;
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                approval.update_user = user_id;
                approval.update_date = now;
                if (request.comment != null)
                    approval.comment = request.comment;

                model.update_date = now;

                _context.T_WORKFLOW_APPROVAL.Update(approval);
                _context.T_WORKFLOW.Update(model);

                _context.SaveChanges();
                tran.Commit();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                tran.Dispose();
                ModelState.AddModelError("", Message_change.FAILURE_001);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Detail(int workflow_no)
        {
            var viewModel = GetDetailView(workflow_no);
            if (viewModel == null)
            {
                return RedirectToAction("Index");
            }
            return View("Detail", viewModel);
        }

        private WorkFlowViewModel CreateViewModel(int workflow_auth, int userId)
        {
            try
            {
                var workflowList = (from w in _context.T_WORKFLOW
                                    join a in _context.T_WORKFLOW_APPROVAL on w.workflow_no equals a.workflow_no
                                    let request_date = (w.status != WorkflowApproveStatus.NONE && w.status != WorkflowApproveStatus.DRAFT) ? w.request_date : default
                                    let approve_date = (w.status == WorkflowApproveStatus.REJECT || w.status == WorkflowApproveStatus.FINISH) ? a.update_date : default
                                    where ((workflow_auth == 0 && w.requester_cd == userId) || (workflow_auth > 0 && (w.requester_cd == userId || a.approver_cd == userId)))
                                    select new WorkFlowModel
                                    {
                                        workflow_no = w.workflow_no,
                                        title = w.title,
                                        description = w.description,
                                        status = w.status,
                                        approve_result = a.approve_result,
                                        request_type = w.request_type,
                                        requester_cd = w.requester_cd,
                                        requester_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == w.requester_cd).staf_name,
                                        approver_cd = a.approver_cd,
                                        approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == a.approver_cd).staf_name,
                                        update_date = w.update_date.ToString("yyyy年M月d日 H時m分"),
                                        request_date = request_date == default ? null : w.request_date.ToString("yyyy年M月d日 H時m分"),
                                        approve_date = approve_date == default ? null : a.update_date.ToString("yyyy年M月d日 H時m分"),
                                        comment = a.comment
                                    }).ToList();

                var model = new WorkFlowViewModel
                {
                    WorkflowList = workflowList
                };

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private WorkFlowDetailViewModel? GetDetailView(int workflow_no)
        {
            try
            {
                var user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);
                var model = (from w in _context.T_WORKFLOW
                             join a in _context.T_WORKFLOW_APPROVAL on w.workflow_no equals a.workflow_no
                             let approve_date = (w.status == WorkflowApproveStatus.REJECT || w.status == WorkflowApproveStatus.FINISH) ? a.update_date : default
                             let request_type = w.request_type.ToString()
                             where w.workflow_no == workflow_no
                             select new WorkFlowDetailViewModel
                             {
                                 workflow_no = w.workflow_no,
                                 title = w.title,
                                 description = w.description,
                                 status = w.status,
                                 request_type = w.request_type,
                                 requester_cd = w.requester_cd,
                                 requester_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == w.requester_cd).staf_name,
                                 approver_cd = a.approver_cd,
                                 approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == a.approver_cd).staf_name,
                                 comment = a.comment,
                                 staffList = _context.M_STAFF
                                     .Where(x => x.retired != 1)
                                     .Select(u => new WorkFlowViewModelStaff
                                     {
                                         staff_cd = u.staf_cd,
                                         staff_name = u.staf_name
                                     })
                                    .ToList(),
                                 requestTypeList = _context.M_DIC
                                    .Where(x => x.dic_kb == DIC_KB.WORKFLOW_REQ_TYPE)
                                    .Select(x => new WorkFlowViewModelRequestType
                                    {
                                        request_type = int.Parse(x.dic_cd),
                                        request_name = x.content
                                    })
                                    .ToList(),
                                 request_type_name = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.WORKFLOW_REQ_TYPE && request_type == x.dic_cd).content,
                                 approve_date = approve_date == default ? null : a.update_date.ToString("yyyy年M月d日 H時m分")
                             }).FirstOrDefault();

                model.fileModel.fileList = _context.T_WORKFLOW_FILE.Where(x => x.workflow_no == workflow_no).ToList();

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private void PrepareViewModel(WorkFlowDetailViewModel model)
        {
            try
            {
                if (model.workflow_no > 0)
                {
                    // 編集のみ
                    model.fileModel.fileList = _context.T_WORKFLOW_FILE.Where(x => x.workflow_no == model.workflow_no).ToList();
                }
                model.staffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new WorkFlowViewModelStaff
                    {
                        staff_cd = u.staf_cd,
                        staff_name = u.staf_name
                    })
                    .ToList();

                model.requestTypeList = _context.M_DIC
                    .Where(x => x.dic_kb == DIC_KB.WORKFLOW_REQ_TYPE)
                    .Select(x => new WorkFlowViewModelRequestType
                    {
                        request_type = int.Parse(x.dic_cd),
                        request_name = x.content
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
                    var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                    var now = DateTime.Now;

                    T_WORKFLOW_FILE record_file = new()
                    {
                        workflow_no = workflow_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        //filepath = Path.Combine(dir_main, file_name),
                        filepath = Path.Combine(workflow_no.ToString(), file_name),
                        filename = file_name,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
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
    }
}