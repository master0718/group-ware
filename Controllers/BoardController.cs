using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using web_groupware.Utilities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Format = web_groupware.Utilities.Format;
using Azure.Core;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618

namespace web_groupware.Controllers
{
    [Authorize]
    public class BoardController : BaseController
    {
        private readonly string _uploadPath;

        public BoardController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) {
            var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.BOARD);
            if (t_dic == null || t_dic.content == null)
            {
                _logger.LogError(Messages.ERROR_PREFIX + Messages.DICTIONARY_FILE_PATH_NO_EXIST, DIC_KB.SAVE_PATH_FILE, DIC_KB_700_DIRECTORY.BOARD);
                throw new Exception(Messages.DICTIONARY_FILE_PATH_NO_EXIST);
            }
            else
            {
                _uploadPath = t_dic.content;
            }
        }

        [HttpGet]
        public IActionResult Index(string? cond_already_checked = null, string? cond_applicant = null, string? cond_category = null, string? cond_keyword = null)
        {
            try
            {
                var model = CreateViewModel(cond_already_checked, cond_applicant, cond_category, cond_keyword);
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
        public IActionResult Index(BoardViewModel model)
        {
            try
            {
                model = CreateViewModel(model.cond_already_checked, model.cond_applicant, model.cond_category, model.cond_keyword);
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

                var viewModel = new BoardDetailViewModel();
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
        public async Task<IActionResult> Create(BoardDetailViewModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
                    PrepareViewModel(request);

                    return View(request);
                }

                for (int i = 0; i < request.File.Count; i++)
                {
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
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

                    // BEGIN T_REQUEST_CONTENT
                    var board_no = GetNextNo(DataTypes.BOARD_NO);
                    var model = new T_BOARD
                    {
                        board_no = board_no,
                        status = request.status,
                        title = request.title,
                        content = request.content,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now,
                        category_cd = request.category_cd,
                        applicant_cd = request.applicant_cd
                    };
                    _context.Add(model);

                    var comment_no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                    var comment = new T_BOARDCOMMENT
                    {
                        board_no = board_no,
                        comment_no = comment_no,
                        message = Board_comment.CREATE,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.Add(comment);

                    string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("Update", "Board", new { id = board_no })}";
                    var t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd.ToString() != user_id).ToListAsync();
                    if (request.applicant_cd != null)
                    {
                        t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd == request.applicant_cd).ToListAsync();
                    }                    
                    for (int i = 0; i < t_staffm.Count; i++)
                    {
                        var info_personal = new T_INFO_PERSONAL();
                        info_personal.info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO);
                        info_personal.parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD;
                        info_personal.first_no = board_no;
                        info_personal.staf_cd = t_staffm[i].staf_cd;
                        info_personal.title = "掲示板";
                        info_personal.content = request.content;
                        info_personal.url = url;
                        info_personal.create_user = user_id;
                        info_personal.create_date = now;
                        info_personal.update_user = user_id;
                        info_personal.update_date = now;
                        _context.T_INFO_PERSONAL.Add(info_personal);
                    }

                    AddFiles(request.work_dir, board_no);

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
        public IActionResult Update(int id, bool isEditable = false)
        {
            try
            {
                var viewModel = GetDetailView(id);
                if (viewModel == null)
                {
                    return Index();
                }

                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                string dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                string dir = Path.Combine(_uploadPath, dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(dir);

                string comment_dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"), "comment");
                string comment_dir = Path.Combine(_uploadPath, comment_dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(comment_dir);

                viewModel.work_dir = dir_work;
                viewModel.comment_work_dir = comment_dir_work;
                viewModel.Upload_file_allowed_extension_1 = UPLOAD_FILE_ALLOWED_EXTENSION.IMAGE_PDF;
                viewModel.is_editable = isEditable;

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
        public async Task<IActionResult> Update(BoardDetailViewModel request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                if (request.File.Count > 5)
                {
                    ModelState.AddModelError("", Messages.MAX_FILE_COUNT_5);
                    ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
                    PrepareViewModel(request);
                    return View(request);
                }
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (request.File[i].Length > Format.FILE_SIZE)
                    {
                        ModelState.AddModelError("", Messages.MAX_FILE_SIZE_20MB);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
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

                var model = _context.T_BOARD.FirstOrDefault(x => x.board_no == request.board_no);
                model.title = request.title;
                model.content = request.content;
                model.update_user = user_id;
                model.update_date = now;
                model.applicant_cd = request.applicant_cd;
                model.status = request.status;
                model.category_cd = request.category_cd;

                _context.T_BOARD.Update(model);

                var comment_no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                var comment = new T_BOARDCOMMENT
                {
                    board_no = request.board_no,
                    comment_no = comment_no,
                    message = Board_comment.UPDATE,
                    create_user = user_id,
                    create_date = now,                    
                    update_user = user_id,
                    update_date = now
                };
                _context.T_BOARDCOMMENT.Add(comment);

                var boardTop = _context.T_BOARD_TOP.FirstOrDefault(x => x.board_no == request.board_no && x.staf_cd == Convert.ToInt32(user_id));
                if (boardTop != null)
                {
                    if (request.show_on_top != true)
                    {
                        _context.Remove(boardTop);
                    }
                    else
                    {
                        boardTop.update_user = user_id;
                        boardTop.update_date = now;
                        _context.Update(boardTop);
                    }
                }
                else
                {
                    if (request.show_on_top == true)
                    {
                        boardTop = new T_BOARD_TOP
                        {
                            board_no = request.board_no,
                            staf_cd = Convert.ToInt32(user_id),
                            create_user = user_id,
                            create_date = now,
                            update_user = user_id,
                            update_date = now
                        };
                        _context.Add(boardTop);
                    }
                }
                var personal = _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                    .Where(m => m.first_no == request.board_no).ToList();
                _context.T_INFO_PERSONAL.RemoveRange(personal);

                string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("Update", "Board", new { id = request.board_no })}";
                var t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd.ToString() != user_id).ToListAsync();
                if (request.applicant_cd != null)
                {
                    t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd == request.applicant_cd).ToListAsync();
                }
                for (int i = 0; i < t_staffm.Count; i++)
                {
                    var info_personal = new T_INFO_PERSONAL();
                    info_personal.info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO);
                    info_personal.parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD;
                    info_personal.first_no = request.board_no;
                    info_personal.staf_cd = t_staffm[i].staf_cd;
                    info_personal.title = "掲示板";
                    info_personal.content = request.content;
                    info_personal.url = url;
                    info_personal.create_user = user_id;
                    info_personal.create_date = now;
                    info_personal.update_user = user_id;
                    info_personal.update_date = now;
                    _context.T_INFO_PERSONAL.Add(info_personal);
                }

                //レコード登録前にmainからファイル削除
                if (request.Delete_files != null)
                {
                    var arr_delete_files = request.Delete_files.Split(':');
                    string dir_main = Path.Combine(_uploadPath, request.board_no.ToString());
                    for (int i = 0; i < arr_delete_files.Length; i++)
                    {
                        if (arr_delete_files[i] != "")
                        {
                            var model_file = _context.T_BOARD_FILE.First(x => x.board_no == request.board_no && x.filename == arr_delete_files[i]);
                            _context.T_BOARD_FILE.Remove(model_file);

                            var filepath = Path.Combine(dir_main, arr_delete_files[i]);
                            System.IO.File.Delete(filepath);
                            // System.IO.File.Delete(Path.ChangeExtension(filepath, "jpeg"));
                        }
                    }
                }

                AddFiles(request.work_dir, request.board_no);

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
        public IActionResult UpdateStatus(int board_no, int status)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var now = DateTime.Now;

                var model = _context.T_BOARD.FirstOrDefault(x => x.board_no == board_no);
                model.status = status;
                model.update_date = now;
                _context.T_BOARD.Update(model);

                var comment_no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                var comment = new T_BOARDCOMMENT
                {
                    board_no = board_no,
                    comment_no = comment_no,
                    message = Board_comment.UPDATE,
                    create_user = user_id,
                    create_date = now,
                    update_user = user_id,
                    update_date = now
                };
                _context.T_BOARDCOMMENT.Add(comment);

                _context.SaveChanges();
                tran.Commit();

                return Json(new { result = "ok" });
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
        public IActionResult UpdateTop(int board_no)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var now = DateTime.Now;

                var boardTop = _context.T_BOARD_TOP.FirstOrDefault(x => x.board_no == board_no && x.staf_cd == Convert.ToInt32(user_id));
                if (boardTop != null)
                {
                    _context.Remove(boardTop);
                }
                else
                {
                    boardTop = new T_BOARD_TOP
                    {
                        board_no = board_no,
                        staf_cd = Convert.ToInt32(user_id),
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.Add(boardTop);
                }

                _context.SaveChanges();
                tran.Commit();

                return Json(new { result = "ok" });
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

        /// <summary>
        /// T_CHECKED更新 日報
        /// </summary>
        /// <returns></returns>
        public IActionResult Check_comment_main(string board_no)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var user_id = User.FindFirst(ClaimTypes.STAF_CD).Value;
                        var btn_text = "";
                        var t_checked_login_user = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD && x.first_no == int.Parse(board_no) && x.second_no == null && x.staf_cd == int.Parse(user_id));
                        if (t_checked_login_user == null)
                        {
                            var now = DateTime.Now;
                            var t_checked_new = new T_CHECKED();
                            t_checked_new.check_no = GetNextNo(DataTypes.CHECK_NO);
                            t_checked_new.parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD;
                            t_checked_new.first_no = int.Parse(board_no);
                            t_checked_new.staf_cd = int.Parse(user_id);
                            t_checked_new.create_user = user_id;
                            t_checked_new.create_date = now;
                            t_checked_new.update_user = user_id;
                            t_checked_new.update_date = now;
                            _context.T_CHECKED.Add(t_checked_new);
                            btn_text = Check_button_text.CANCEL;
                        }
                        else
                        {
                            _context.T_CHECKED.Remove(t_checked_login_user);
                            btn_text = Check_button_text.CHECK;
                        }
                        _context.SaveChanges();
                        tran.Commit();
                        var result = new List<object>();
                        result.Add(btn_text);
                        var t_checked = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD && x.first_no == int.Parse(board_no) && x.second_no == null);
                        result.Add(t_checked.Count() + "名");
                        var list = t_checked.GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y }).SelectMany(um => um.y.DefaultIfEmpty()).Select(zz => zz.staf_name).ToList();
                        result.Add(list);
                        return Json(result);
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// T_CHECKED更新 日報
        /// </summary>
        /// <returns></returns>
        public IActionResult Check_comment(string board_no, string comment_no)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var user_id = User.FindFirst(ClaimTypes.STAF_CD).Value;
                        var btn_text = "";
                        var t_checked_login_user = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD && x.first_no == int.Parse(board_no) && x.second_no == int.Parse(comment_no) && x.staf_cd == int.Parse(user_id));
                        if (t_checked_login_user == null)
                        {
                            var now = DateTime.Now;
                            var t_checked_new = new T_CHECKED();
                            t_checked_new.check_no = GetNextNo(DataTypes.CHECK_NO);
                            t_checked_new.parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD;
                            t_checked_new.first_no = int.Parse(board_no);
                            t_checked_new.second_no = int.Parse(comment_no);
                            t_checked_new.staf_cd = int.Parse(user_id);
                            t_checked_new.create_user = user_id;
                            t_checked_new.create_date = now;
                            t_checked_new.update_user = user_id;
                            t_checked_new.update_date = now;
                            _context.T_CHECKED.Add(t_checked_new);
                            btn_text = Check_button_text.CANCEL;
                        }
                        else
                        {
                            _context.T_CHECKED.Remove(t_checked_login_user);
                            btn_text = Check_button_text.CHECK;
                        }
                        _context.SaveChanges();
                        tran.Commit();
                        var result = new List<object>();
                        result.Add(btn_text);
                        var t_checked = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD && x.first_no == int.Parse(board_no) && x.second_no == int.Parse(comment_no));
                        result.Add(t_checked.Count() + "名");
                        var list = t_checked.GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y }).SelectMany(um => um.y.DefaultIfEmpty()).Select(zz => zz.staf_name).ToList();
                        result.Add(list);
                        return Json(result);
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }


        [HttpPost]
        public IActionResult AddComment(int board_no, string message, string work_dir)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                var now = DateTime.Now;

                var model = new T_BOARDCOMMENT
                {
                    board_no = board_no,
                    comment_no = no,
                    message = message,
                    create_user = user_id,
                    create_date = now,
                    update_user = user_id,
                    update_date = now
                };
                _context.Add(model);

                string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("Update", "Board", new { id = board_no })}";
                var t_board = _context.T_BOARD.FirstOrDefault(x => x.board_no == board_no);
                if (t_board.update_user != user_id)
                {
                    var record_read = new T_INFO_PERSONAL();
                    record_read.info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO);
                    record_read.parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD;
                    record_read.first_no = board_no;
                    record_read.second_no = no;
                    record_read.staf_cd = int.Parse(t_board.update_user);
                    record_read.title = "掲示板コメント";
                    record_read.content = model.message;
                    record_read.url = url;
                    record_read.create_user = user_id;
                    record_read.create_date = now;
                    record_read.update_user = user_id;
                    record_read.update_date = now;
                    _context.T_INFO_PERSONAL.Add(record_read);
                }

                var t_board_read = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD && x.first_no == board_no).ToList();
                for (int i = 0; i < t_board_read.Count(); i++)
                {
                    if (t_board_read[i].staf_cd.ToString() == user_id) continue;
                    var record_read = new T_INFO_PERSONAL();
                    record_read.info_personal_no = GetNextNo(Utilities.DataTypes.INFO_PERSONAL_NO);
                    record_read.parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD;
                    record_read.first_no = board_no;
                    record_read.second_no = no;
                    record_read.staf_cd = t_board_read[i].staf_cd;
                    record_read.title = "掲示板コメント";
                    record_read.content = model.message;
                    record_read.url = url;
                    record_read.create_user = user_id;
                    record_read.create_date = now;
                    record_read.update_user = user_id;
                    record_read.update_date = now;
                    _context.T_INFO_PERSONAL.Add(record_read);
                }

                AddCommentFiles(work_dir, no, board_no);

                _context.SaveChanges();
                tran.Commit();

                var commentFiles = _context.T_BOARDCOMMENT_FILE.Where(x => x.board_no == board_no && x.comment_no == no).ToList();

                string comment_dir_work = Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                string comment_dir = Path.Combine(_uploadPath, comment_dir_work);
                //workディレクトリの作成
                Directory.CreateDirectory(comment_dir);

                return Json(new { model.comment_no, update_date = model.update_date.ToString("yyyy年M月d日 H時m分"), files = commentFiles, comment_work_dir = comment_dir });
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
        public IActionResult GetMoreCommentList(int board_no, int last_comment_no)
        {
            try
            {
                var commentList = (from c in _context.T_BOARDCOMMENT
                                   where c.board_no == board_no && c.comment_no > last_comment_no
                                   orderby c.update_date ascending
                                   let v = Convert.ToInt32(c.update_user)
                                   select new BoardCommentModel
                                   {
                                       board_no = c.board_no,
                                       comment_no = c.comment_no,
                                       message = c.message,
                                       registrant_cd = v,
                                       registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == v).staf_name,
                                       register_date = c.update_date.ToString("yyyy年M月d日 H時m分"),
                                       CommentFileDetailList = (List<T_BOARDCOMMENT_FILE>)(from f in _context.T_BOARDCOMMENT_FILE
                                                                                           where f.board_no == board_no && f.comment_no == c.comment_no
                                                                                           select new T_BOARDCOMMENT_FILE
                                                                                           {
                                                                                               board_no = f.board_no,
                                                                                               comment_no = f.comment_no,
                                                                                               file_no = f.file_no,
                                                                                               filename = f.filename,
                                                                                               filepath = f.filepath,
                                                                                           })
                                   }).Take(5).ToList();
                return Json(new { commentList });
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Delete(int board_no)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var model = _context.T_BOARD.FirstOrDefault(x => x.board_no == board_no);
                if (model != null)
                    _context.T_BOARD.Remove(model);

                var comment = _context.T_BOARDCOMMENT.Where(x => x.board_no == board_no).ToList();
                if (comment != null)
                    _context.T_BOARDCOMMENT.RemoveRange(comment);

                var user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);
                var personal = _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                    .Where(m => m.first_no == board_no).ToList();
                _context.T_INFO_PERSONAL.RemoveRange(personal);

                var boardTop = _context.T_BOARD_TOP.Where(x => x.board_no == board_no).ToList();
                if (boardTop != null)
                    _context.T_BOARD_TOP.RemoveRange(boardTop);

                var checkedList = _context.T_CHECKED
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                    .Where(m => m.first_no == board_no).ToList();
                _context.T_CHECKED.RemoveRange(checkedList);

                var model_files = _context.T_BOARD_FILE.Where(x => x.board_no == board_no).ToList();
                if (model_files != null && model_files.Count > 0)
                {
                    _context.T_BOARD_FILE.RemoveRange(model_files);
                    string dir_main = Path.Combine(_uploadPath, board_no.ToString());
                    Directory.Delete(dir_main, true);
                }

                var comment_files = _context.T_BOARDCOMMENT_FILE.Where(x => x.board_no == board_no).ToList();
                if (comment_files != null && comment_files.Count > 0)
                {
                    _context.T_BOARDCOMMENT_FILE.RemoveRange(comment_files);
                    if (model_files == null && model_files.Count == 0)
                    {
                        string dir_main = Path.Combine(_uploadPath, board_no.ToString());
                        Directory.Delete(dir_main, true);
                    }
                }

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

        private BoardViewModel CreateViewModel(string cond_already_checked, string? cond_applicant, string? cond_category, string? cond_keyword)
        {
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var boardList = (from b in _context.T_BOARD
                                 let p = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD && x.first_no == b.board_no && x.second_no == null && x.staf_cd == Convert.ToInt32(user_id))
                                 let t = _context.T_BOARD_TOP.FirstOrDefault(x => x.board_no == b.board_no && x.staf_cd == Convert.ToInt32(user_id))
                                 select new BoardModel
                                 {
                                     board_no = b.board_no,
                                     status = b.status,
                                     category_cd = b.category_cd.ToString(),
                                     title = b.title,
                                     content = b.content,
                                     registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == Convert.ToInt32(b.update_user)).staf_name,
                                     register_date = b.update_date.ToString("yyyy年M月d日"),
                                     applicant_cd = b.applicant_cd.ToString(),
                                     applicant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == b.applicant_cd).staf_name,
                                     already_checked = p != null ? 1 : 0,
                                     show_on_top = t != null,
                                 }).OrderByDescending(x => x.show_on_top).ToList();
                if (cond_already_checked == "1") // "未確認"
                {
                    boardList = boardList.Where(x => x.already_checked == 0).ToList();
                }
                else if (cond_already_checked == "2") // "確認済"
                {
                    boardList = boardList.Where(x => x.already_checked == 1).ToList();
                }
                if (!cond_applicant.IsNullOrEmpty() && cond_applicant != "0")
                {
                    boardList = boardList.Where(x => x.applicant_cd == cond_applicant).ToList();
                }
                if (!cond_category.IsNullOrEmpty() && cond_category != "0")
                {
                    boardList = boardList.Where(x => x.category_cd == cond_category).ToList();
                }
                if (cond_keyword != null)
                {
                    boardList = boardList.Where(x => x.title.Contains(cond_keyword) || x.content.Contains(cond_keyword)).ToList();
                }
                var model = new BoardViewModel
                {
                    BoardList = boardList,
                    list_applicant = _context.M_STAFF
                        .Where(x => x.retired != 1)
                        .Select(u => new SelectListItem
                        {
                            Value = u.staf_cd.ToString(),
                            Text = u.staf_name
                        })
                        .ToList(),
                    list_category = _context.M_DIC
                        .Where(x => x.dic_kb == DIC_KB.BOARD_CATEGORY)
                        .Select(u => new SelectListItem
                        {
                            Value = u.dic_cd,
                            Text = u.content
                        })
                        .ToList()
                };
                model.list_applicant.Insert(0, new SelectListItem { Value = "0", Text = "全て" });
                model.list_category.Insert(0, new SelectListItem { Value = "0", Text = "全て" });
                model.cond_already_checked = cond_already_checked;
                model.cond_applicant = cond_applicant;
                model.cond_category = cond_category;
                model.cond_keyword = cond_keyword;

                for (var i = 0; i < BoardStatus.All.Length; i++)
                {
                    var count = model.BoardList.Count(x => x.status == i);
                    model.CountList.Add(count);
                }
                
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private BoardDetailViewModel? GetDetailView(int board_no)
        {
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var iUser_id = Convert.ToInt32(user_id);
                var model = (from b in _context.T_BOARD
                             let t = _context.T_BOARD_TOP.FirstOrDefault(x => x.board_no == board_no && x.staf_cd == iUser_id)
                             where b.board_no == board_no
                             select new BoardDetailViewModel
                             {
                                 board_no = b.board_no,
                                 status = b.status,
                                 category_cd = Convert.ToInt32(b.category_cd),
                                 title = b.title,
                                 content = b.content,
                                 registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == iUser_id).staf_name,
                                 register_date = b.update_date.ToString("yyyy年M月d日"),
                                 notifier_cd = b.notifier_cd,
                                 notifier_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == b.notifier_cd).staf_name,
                                 applicant_cd = b.applicant_cd ?? 0,
                                 show_on_top = t != null,
                                 StaffList = _context.M_STAFF
                                     .Where(x => x.retired != 1)
                                     .Select(u => new BoardViewModelStaff
                                     {
                                         staff_cd = u.staf_cd,
                                         staff_name = u.staf_name
                                     })
                                    .ToList(),
                                 CategoryList = _context.M_DIC
                                    .Where(x => x.dic_kb == DIC_KB.BOARD_CATEGORY)
                                    .Select(u => new BoardViewModelCategory
                                    {
                                        category_cd = Convert.ToInt32(u.dic_cd),
                                        category_name = u.content
                                    })
                                    .ToList()
                             }).FirstOrDefault();

                model.fileModel.fileList = _context.T_BOARD_FILE.Where(x => x.board_no == board_no).ToList();
                var boardRead = _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                    .Where(m => m.first_no == board_no && m.staf_cd == iUser_id)
                    .FirstOrDefault();
                using IDbContextTransaction tran = _context.Database.BeginTransaction();
                try
                {
                    if (boardRead != null)
                    {
                        boardRead.already_read = true;
                        _context.T_INFO_PERSONAL.Update(boardRead);
                    }
                    else
                    {
                        var now = DateTime.Now;
                        string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("Update", "Board", new { id = board_no })}";
                        boardRead = new T_INFO_PERSONAL
                        {
                            info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO),
                            parent_id = INFO_PERSONAL_PARENT_ID.T_BOARD,
                            first_no = board_no,
                            second_no = 0,
                            third_no = 0,
                            staf_cd = iUser_id,
                            already_read = true,
                            title = model.title,
                            content = model.content,
                            url = url,
                            added = false,
                            create_user = user_id,
                            create_date = now,
                            update_user = user_id,
                            update_date = now
                        };
                        _context.T_INFO_PERSONAL.Add(boardRead);
                    }
                    _context.SaveChanges();
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    return null;
                }
                var boardChecked = _context.T_CHECKED
                    .Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                    .Where(x => x.first_no == board_no && x.staf_cd == iUser_id && x.second_no == null)
                    .FirstOrDefault();
                model.already_checked = boardChecked != null;

                var list_t_checked_main = _context.T_CHECKED
                    .Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                    .Where(x => x.first_no == board_no && x.second_no == null)
                    .GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y })
                    .SelectMany(um => um.y.DefaultIfEmpty())
                    .Select(zz => zz.staf_name)
                    .ToList();
                model.check_count = list_t_checked_main.Count() + "名";
                model.list_check_member = list_t_checked_main;

                var commentList = (from c in _context.T_BOARDCOMMENT
                                where c.board_no == board_no
                                orderby c.update_date ascending
                                let v = Convert.ToInt32(c.update_user)
                                let commentChecked = _context.T_CHECKED
                                    .Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                                    .Where(x => x.first_no == board_no && x.second_no == c.comment_no && x.staf_cd == iUser_id)
                                    .FirstOrDefault()
                                let comment_already_checked = commentChecked != null
                                let list_t_checked_comment = _context.T_CHECKED
                                    .Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BOARD)
                                    .Where(x => x.first_no == board_no && x.second_no == c.comment_no)
                                    .GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y })
                                    .SelectMany(um => um.y.DefaultIfEmpty())
                                    .Select(zz => zz.staf_name)
                                    .ToList()
                                select new BoardCommentModel
                                {
                                    board_no = c.board_no,
                                    comment_no = c.comment_no,
                                    message = c.message,
                                    registrant_cd = v,
                                    registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == v).staf_name,
                                    register_date = c.update_date.ToString("yyyy年M月d日 H時m分"),
                                    CommentFileDetailList = (from f in _context.T_BOARDCOMMENT_FILE
                                                                where f.board_no == board_no && f.comment_no == c.comment_no
                                                                select new T_BOARDCOMMENT_FILE
                                                                {
                                                                    board_no = f.board_no,
                                                                    comment_no = f.comment_no,
                                                                    file_no = f.file_no,
                                                                    filename = f.filename,
                                                                    filepath = f.filepath,
                                                                }).ToList(),
                                    comment_check_count = list_t_checked_comment.Count() + "名",
                                    comment_already_checked = comment_already_checked,
                                    comment_list_check_member = list_t_checked_comment
                                }).Take(5).ToList();

                var commentCount = (from c in _context.T_BOARDCOMMENT
                                    where c.board_no == board_no
                                    orderby c.update_date ascending
                                    select c.comment_no).Count();

                model.CommentList = commentList;
                model.commentTotalCount = commentCount;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private void PrepareViewModel(BoardDetailViewModel model)
        {
            try
            {
                if (model.board_no > 0)
                {
                    // 編集のみ
                    model.fileModel.fileList = _context.T_BOARD_FILE.Where(x => x.board_no == model.board_no).ToList();
                }
                model.StaffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new BoardViewModelStaff
                    {
                        staff_cd = u.staf_cd,
                        staff_name = u.staf_name
                    })
                    .ToList();
                model.CategoryList = _context.M_DIC
                    .Where(x => x.dic_kb == DIC_KB.BOARD_CATEGORY)
                    .Select(u => new BoardViewModelCategory
                    {
                        category_cd = Convert.ToInt32(u.dic_cd),
                        category_name = u.content
                    })
                    .ToList();
                model.status = BoardStatus.UPCOMING;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        protected async void AddFiles(string work_dir, int board_no)
        {
            try
            {
                //ディレクトリ設定
                string dir_main = Path.Combine(_uploadPath, board_no.ToString());
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

                    T_BOARD_FILE record_file = new()
                    {
                        board_no = board_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        //filepath = Path.Combine(dir_main, file_name),
                        filepath = Path.Combine(board_no.ToString(), file_name),
                        filename = file_name,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    await _context.T_BOARD_FILE.AddAsync(record_file);

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

        protected async void AddCommentFiles(string work_dir, int comment_no, int board_no)
        {
            try
            {
                //ディレクトリ設定
                string dir_main = Path.Combine(_uploadPath, board_no.ToString(), comment_no.ToString());
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
                    T_BOARDCOMMENT_FILE record_file = new()
                    {
                        board_no = board_no,
                        comment_no = comment_no,
                        file_no = GetNextNo(DataTypes.BOARDCOMMENT_FILE_NO),
                        /*filepath = Path.Combine(dir_main, file_name),*/
                        filepath = Path.Combine(board_no.ToString(), comment_no.ToString(), file_name),
                        filename = file_name,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    await _context.T_BOARDCOMMENT_FILE.AddAsync(record_file);

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