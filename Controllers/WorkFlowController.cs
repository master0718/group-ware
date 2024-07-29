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

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618, CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class WorkFlowController : BaseController
    {
        private readonly IWebHostEnvironment _environment;
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

        public IActionResult Index(int status = 0, string? keyword = null)
        {
            try
            {
                var userId = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var user_auth = _context.M_STAFF_AUTH.FirstOrDefault(m => m.staf_cd == int.Parse(userId));
                if (userId == null || user_auth == null || _context.T_WORKFLOW == null)
                {
                    return View(new WorkFlowViewModel());
                }
                var model = CreateViewModel(user_auth.workflow_auth, int.Parse(userId), status, keyword);
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
        public IActionResult Filter(int filter_status = 0, string? filter_keyword = null)
        {
            TempData["filter_status"] = filter_status;
            TempData["filter_keyword"] = filter_keyword;

            var routeValues = new { status = filter_status, keyword = filter_keyword };
            return RedirectToAction("Index", routeValues);
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
                for (int i = 0; i < request.File.Count; i++)
                {
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
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.Add(model);

                    var top_approval_model = new T_WORKFLOW_TOP_APPROVAL
                    {
                        workflow_no = workflow_no,
                        approver_cd = request.top_approver_cd,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };

                    _context.Add(top_approval_model);

                    if (request.approver_cd1 != null)
                    {
                        var approval_model = new T_WORKFLOW_APPROVAL
                        {
                            workflow_no = workflow_no,
                            approver_cd = request.approver_cd1 ?? 0,
                            create_user = user_id,
                            create_date = now,
                            update_user = user_id,
                            update_date = now
                        };

                        _context.Add(approval_model);
                    }

                    if (request.approver_cd2 != null)
                    {
                        var approval_model = new T_WORKFLOW_APPROVAL
                        {
                            workflow_no = workflow_no,
                            approver_cd = request.approver_cd2 ?? 0,
                            create_user = user_id,
                            create_date = now,
                            update_user = user_id,
                            update_date = now
                        };

                        _context.Add(approval_model);
                    }

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
                if (request.approver_cd1 != null && request.approver_cd2 == null)
                {
                    ModelState.AddModelError("", Messages.WORKFLOW_APPROVAL_APPROVER2_REQUIRED);
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
                for (int i = 0; i < request.File.Count; i++)
                {
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

                var top_approvals = _context.T_WORKFLOW_TOP_APPROVAL.Where(x => x.workflow_no == request.workflow_no).ToList();
                if (top_approvals != null)
                    _context.T_WORKFLOW_TOP_APPROVAL.RemoveRange(top_approvals);

                var top_approval_model = new T_WORKFLOW_TOP_APPROVAL
                {
                    workflow_no = request.workflow_no,
                    approver_cd = request.top_approver_cd,
                    create_user = user_id,
                    create_date = now,
                    update_user = user_id,
                    update_date = now
                };
                _context.Add(top_approval_model);

                if (request.approver_cd1 != null)
                {
                    var approval_model = new T_WORKFLOW_APPROVAL
                    {
                        workflow_no = request.workflow_no,
                        approver_cd = request.approver_cd1 ?? 0,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.Add(approval_model);
                }

                if (request.approver_cd2 != null)
                {
                    var approval_model = new T_WORKFLOW_APPROVAL
                    {
                        workflow_no = request.workflow_no,
                        approver_cd = request.approver_cd2 ?? 0,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.Add(approval_model);
                }

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

                var top_approvals = _context.T_WORKFLOW_TOP_APPROVAL.Where(x => x.workflow_no == request.workflow_no).ToList();
                if (top_approvals != null)
                    _context.T_WORKFLOW_TOP_APPROVAL.RemoveRange(top_approvals);

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

                var top_approval = _context.T_WORKFLOW_TOP_APPROVAL.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                if (top_approval != null)
                {
                    top_approval.approve_result = WorkflowApproveResult.NONE;
                    top_approval.comment = "";
                    _context.T_WORKFLOW_TOP_APPROVAL.Update(top_approval);
                }

                var approvals = _context.T_WORKFLOW_APPROVAL.Where(x => x.workflow_no == request.workflow_no).ToList();
                if (approvals != null)
                {
                    foreach (var approval in approvals)
                    {
                        approval.approve_result = WorkflowApproveResult.NONE;
                        approval.comment = "";
                    }
                    _context.T_WORKFLOW_APPROVAL.UpdateRange(approvals);
                }

                if (approvals == null || approvals.Count() == 0)
                {
                    model.status = WorkflowApproveStatus.TOP_APPROVE;
                }
                else
                {
                    model.status = WorkflowApproveStatus.REQUEST;
                }
                model.update_date = now;
                _context.T_WORKFLOW.Update(model);

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
            try
            {
                var viewModel = GetApproveDetailView(workflow_no);
                if (viewModel == null)
                {
                    return Index();
                }

                return View("Approve", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        public IActionResult Accept(ApproveDetailViewModel request)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var model = _context.T_WORKFLOW.FirstOrDefault(x => x.workflow_no == request.workflow_no);
                if (request.is_top_approver == 0)
                {
                    if (model.status != WorkflowApproveStatus.REQUEST && model.status != WorkflowApproveStatus.APPROVE)
                    {
                        ModelState.AddModelError("", Messages.WORKFLOW_APPROVAL_NOT_APPROVAL);
                        return Accept(request.workflow_no);
                    }
                } else
                {
                    if (model.status == WorkflowApproveStatus.FINISH)
                    {
                        ModelState.AddModelError("", Messages.WORKFLOW_APPROVAL_ALREADY_FINISHED);
                        return Accept(request.workflow_no);
                    }
                    else if (model.status == WorkflowApproveStatus.REJECT)
                    {
                        ModelState.AddModelError("", Messages.WORKFLOW_APPROVAL_ALREADY_REJECTED);
                        return Accept(request.workflow_no);
                    }
                }

                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var iUser_id = Convert.ToInt32(user_id);
                dynamic approval = null;

                if (request.is_top_approver == 0) // approval
                {
                    approval = _context.T_WORKFLOW_APPROVAL.FirstOrDefault(x => x.workflow_no == request.workflow_no && x.approver_cd == Convert.ToInt32(user_id));

                    if (request.is_accept == 0)
                    {
                        approval.approve_result = WorkflowApproveResult.REJECT;
                    } else
                    {
                        approval.approve_result = WorkflowApproveResult.ACCEPT;
                    }

                    var no_approvers = _context.T_WORKFLOW_APPROVAL
                        .Where(x => x.workflow_no == model.workflow_no && x.approver_cd != iUser_id)
                        .Where(x => x.approve_result == WorkflowApproveResult.NONE)
                        .ToList();
                    if (no_approvers.Count > 0)
                    {
                        // 未承認処理の承認者
                        if (model.status == WorkflowApproveStatus.REQUEST)
                            model.status = WorkflowApproveStatus.APPROVE;
                    }
                    else
                    {
                        // 全員が承認処理済み
                        var rejected_approvers = _context.T_WORKFLOW_APPROVAL
                            .Where(x => x.workflow_no == model.workflow_no && x.approver_cd != iUser_id)
                            .Where(x => x.approve_result == WorkflowApproveResult.REJECT)
                            .ToList();
                        if (rejected_approvers.Count > 0)
                        {
                            // すでに他人が否決
                            model.status = WorkflowApproveStatus.REJECT;
                        }
                        else
                        {
                            // 別の方はすべて承認済み
                            model.status = request.is_accept == 1 ? WorkflowApproveStatus.TOP_APPROVE : WorkflowApproveStatus.REJECT;
                        }
                    }
                } else // top approval
                {
                    approval = _context.T_WORKFLOW_TOP_APPROVAL.FirstOrDefault(x => x.workflow_no == request.workflow_no && x.approver_cd == Convert.ToInt32(user_id));

                    if (request.is_accept == 0)
                    {
                        model.status = WorkflowApproveStatus.REJECT;
                        approval.approve_result = WorkflowApproveResult.REJECT;
                    }
                    else
                    {
                        model.status = WorkflowApproveStatus.FINISH;
                        approval.approve_result = WorkflowApproveResult.ACCEPT;
                    }
                }

                var now = DateTime.Now;
                approval.update_user = user_id;
                approval.update_date = now;
                if (request.comment != null)
                    approval.comment = request.comment;

                model.update_date = now;

                if (request.is_top_approver == 0)
                    _context.T_WORKFLOW_APPROVAL.Update(approval);
                else
                    _context.T_WORKFLOW_TOP_APPROVAL.Update(approval);

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
            try
            {
                var viewModel = GetDetailView(workflow_no);
                if (viewModel == null)
                {
                    return Index();
                }

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private WorkFlowViewModel CreateViewModel(int workflow_auth, int userId, int selected_status = 0, string? keyword = null)
        {
            try
            {
                var myUserId = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);

                var workflowList__ = (from w in _context.T_WORKFLOW
                                    let approver_me = _context.T_WORKFLOW_APPROVAL.Where(x => x.workflow_no == w.workflow_no && x.approver_cd == userId).Count()
                                    let top_approver_me = _context.T_WORKFLOW_TOP_APPROVAL.Where(x => x.workflow_no == w.workflow_no && x.approver_cd == userId).Count()
                                    //where (workflow_auth == 0 && w.requester_cd == userId) || (workflow_auth == 1 && approver_me > 0) || (workflow_auth == 2 && top_approver_me > 0)
                                    where (w.requester_cd == userId) || (workflow_auth >= 1 && approver_me > 0) || (workflow_auth == 2 && top_approver_me > 0)
                                    select w);

                if (keyword != null && keyword != "")
                {
                    workflowList__ = workflowList__.Where(x => x.title.Contains(keyword));
                }

                if (selected_status != 0)
                {
                    workflowList__ = workflowList__.Where(x => x.status == selected_status);
                }

                var workflowList_ = workflowList__.ToList();

                var workflowList = new List<WorkFlowModel>();
                foreach (var w in workflowList_)
                {
                    var workflow = new WorkFlowModel
                    {
                        workflow_no = w.workflow_no,
                        title = w.title,
                        description = w.description,
                        status = w.status,
                        request_type = w.request_type,
                        requester_cd = w.requester_cd,
                        requester_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == w.requester_cd).staf_name,
                        //request_date = w.request_date == null ? "" : w.request_date?.ToString("yyyy年M月d日 H時m分"),
                        update_date = w.update_date.ToString("yyyy年M月d日 H時m分"),
                    };

                    var top_approver = _context.T_WORKFLOW_TOP_APPROVAL.FirstOrDefault(x => x.workflow_no == w.workflow_no);
                    if (top_approver != null)
                    {
                        workflow.top_approver = new ApproverModel
                        {
                            approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == top_approver.approver_cd).staf_name,
                            approve_date = top_approver.approve_result == WorkflowApproveResult.NONE ? "" : top_approver.update_date.ToString("yyyy年M月d日 H時m分"),
                            comment = top_approver.comment,
                            approve_result = top_approver.approve_result,
                        };

                        if (top_approver.approver_cd == myUserId)
                        {
                            workflow.my_approval_result = top_approver.approve_result;
                            workflow.is_top_approver = 1;
                        }
                    }

                    var approvers = _context.T_WORKFLOW_APPROVAL.Where(x => x.workflow_no == w.workflow_no).ToList();
                    if (approvers != null)
                    {
                        T_WORKFLOW_APPROVAL? first_approver = null;
                        T_WORKFLOW_APPROVAL? second_approver = null;
                        if (approvers.Count == 1) // second approver
                        {
                            second_approver = approvers[0];
                        } else if (approvers.Count == 2) // first, second arppvoer
                        {
                            first_approver = approvers[0];
                            second_approver = approvers[1];
                        }

                        if (first_approver != null)
                        {
                            workflow.approver1 = new ApproverModel
                            {
                                approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == first_approver.approver_cd).staf_name,
                                approve_date = first_approver?.approve_result == WorkflowApproveResult.NONE ? "" : first_approver.update_date.ToString("yyyy年M月d日 H時m分"),
                                comment = first_approver.comment,
                                approve_result = first_approver.approve_result,
                            };

                            if (first_approver.approver_cd == myUserId)
                            {
                                workflow.my_approval_result = first_approver.approve_result;
                                workflow.is_top_approver = 0;
                            }
                        }
                        if (second_approver != null)
                        {
                            workflow.approver2 = new ApproverModel
                            {
                                approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == second_approver.approver_cd).staf_name,
                                approve_date = second_approver?.approve_result == WorkflowApproveResult.NONE ? "" : second_approver.update_date.ToString("yyyy年M月d日 H時m分"),
                                comment = second_approver.comment,
                                approve_result = second_approver.approve_result,
                            };

                            if (second_approver.approver_cd == myUserId)
                            {
                                workflow.my_approval_result = second_approver.approve_result;
                                workflow.is_top_approver = 0;
                            }
                        }
                    }

                    workflowList.Add(workflow);
                }

                var model = new WorkFlowViewModel
                {
                    WorkflowList = workflowList,
                    selectedStatus = selected_status,
                    keyword = keyword
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
               
                var model_ = (from w in _context.T_WORKFLOW
                                where w.workflow_no == workflow_no
                                select w).FirstOrDefault();

                var model = new WorkFlowDetailViewModel
                {
                    workflow_no = model_.workflow_no,
                    title = model_.title,
                    description = model_.description,
                    status = model_.status,
                    request_type = model_.request_type,
                    requester_cd = model_.requester_cd,
                    requester_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == model_.requester_cd).staf_name,

                    requestTypeList = _context.M_DIC
                                        .Where(x => x.dic_kb == DIC_KB.WORKFLOW_REQ_TYPE)
                                        .Select(x => new WorkFlowViewModelRequestType
                                        {
                                            request_type = int.Parse(x.dic_cd),
                                            request_name = x.content
                                        })
                                        .ToList(),

                    request_type_name = (from d in _context.M_DIC
                                         where d.dic_kb == DIC_KB.WORKFLOW_REQ_TYPE && model_.request_type.ToString() == d.dic_cd
                                         select d.content)
                                        .FirstOrDefault(),

                    staffList = (from s in _context.M_STAFF
                                       let auth = (from a in _context.M_STAFF_AUTH where a.staf_cd == s.staf_cd select a.workflow_auth).FirstOrDefault()
                                       where s.retired != 1 && s.staf_cd != user_id && auth > 0
                                       select new WorkFlowViewModelStaff
                                       {
                                           staff_cd = s.staf_cd,
                                           staff_name = s.staf_name
                                       }
                                       ).ToList(),

                    top_staffList = (from s in _context.M_STAFF
                                           let auth = (from a in _context.M_STAFF_AUTH where a.staf_cd == s.staf_cd select a.workflow_auth).FirstOrDefault()
                                           where s.retired != 1 && s.staf_cd != user_id && auth == 2
                                           select new WorkFlowViewModelStaff
                                           {
                                               staff_cd = s.staf_cd,
                                               staff_name = s.staf_name
                                           }
                                       ).ToList()
                };

                var top_approver = _context.T_WORKFLOW_TOP_APPROVAL.FirstOrDefault(x => x.workflow_no == model_.workflow_no);
                if (top_approver != null)
                {
                    model.top_approver = new ApproverModel
                    {
                        approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == top_approver.approver_cd).staf_name,
                        approve_date = top_approver.approve_result == WorkflowApproveResult.NONE ? "" : top_approver.update_date.ToString("yyyy年M月d日 H時m分"),
                        comment = top_approver.comment,
                        approve_result = top_approver.approve_result,
                    };
                    model.top_approver_cd = top_approver.approver_cd;
                }

                var approvers = _context.T_WORKFLOW_APPROVAL.Where(x => x.workflow_no == model_.workflow_no).ToList();
                if (approvers != null)
                {
                    T_WORKFLOW_APPROVAL? first_approver = null;
                    T_WORKFLOW_APPROVAL? second_approver = null;
                    if (approvers.Count == 1) // second approver
                    {
                        second_approver = approvers[0];
                    }
                    else if (approvers.Count == 2) // first, second arppvoer
                    {
                        first_approver = approvers[0];
                        second_approver = approvers[1];
                    }

                    if (first_approver != null)
                    {
                        model.approver1 = new ApproverModel
                        {
                            approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == first_approver.approver_cd).staf_name,
                            approve_date = first_approver?.approve_result == WorkflowApproveResult.NONE ? "" : first_approver.update_date.ToString("yyyy年M月d日 H時m分"),
                            comment = first_approver.comment,
                            approve_result = first_approver.approve_result,
                        };
                        model.approver_cd1 = first_approver.approver_cd;
                    }
                    if (second_approver != null)
                    {
                        model.approver2 = new ApproverModel
                        {
                            approver_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == second_approver.approver_cd).staf_name,
                            approve_date = second_approver?.approve_result == WorkflowApproveResult.NONE ? "" : second_approver.update_date.ToString("yyyy年M月d日 H時m分"),
                            comment = second_approver.comment,
                            approve_result = second_approver.approve_result,
                        };
                        model.approver_cd2 = second_approver.approver_cd;
                    }
                }

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

        private ApproveDetailViewModel? GetApproveDetailView(int workflow_no)
        {
            try
            {
                var user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);

                var model_ = (from w in _context.T_WORKFLOW
                              where w.workflow_no == workflow_no
                              select w).FirstOrDefault();

                var model = new ApproveDetailViewModel
                {
                    workflow_no = model_.workflow_no,
                    title = model_.title,
                    description = model_.description,
                    request_type = model_.request_type,
                    requester_cd = model_.requester_cd,
                    requester_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == model_.requester_cd).staf_name,

                    request_type_name = (from d in _context.M_DIC
                                         where d.dic_kb == DIC_KB.WORKFLOW_REQ_TYPE && model_.request_type.ToString() == d.dic_cd
                                         select d.content)
                                        .FirstOrDefault(),
                    is_top_approver = _context.T_WORKFLOW_TOP_APPROVAL.Where(x => x.workflow_no == workflow_no && x.approver_cd == user_id).Count() > 0 ? 1 : 0
                };

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
                var user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);
                if (model.workflow_no > 0)
                {
                    // 編集のみ
                    model.fileModel.fileList = _context.T_WORKFLOW_FILE.Where(x => x.workflow_no == model.workflow_no).ToList();
                }

                model.staffList = (from s in _context.M_STAFF
                                   let auth = (from a in _context.M_STAFF_AUTH where a.staf_cd == s.staf_cd select a.workflow_auth).FirstOrDefault()
                                   where s.retired != 1 && s.staf_cd != user_id && auth > 0
                                   select new WorkFlowViewModelStaff 
                                   {
                                       staff_cd = s.staf_cd,
                                       staff_name = s.staf_name
                                   }
                                   ).ToList();

                model.top_staffList = (from s in _context.M_STAFF
                                   let auth = (from a in _context.M_STAFF_AUTH where a.staf_cd == s.staf_cd select a.workflow_auth).FirstOrDefault()
                                   where s.retired != 1 && s.staf_cd != user_id && auth == 2
                                   select new WorkFlowViewModelStaff
                                   {
                                       staff_cd = s.staf_cd,
                                       staff_name = s.staf_name
                                   }
                                   ).ToList();

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
    }
}