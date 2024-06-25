using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using web_groupware.Utilities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Format = web_groupware.Utilities.Format;

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
        
        public IActionResult Index()
        {
            try
            {
                var model = CreateViewModel();
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
                var list_allowed_file_extentions = new List<string>() { ".pdf" };
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (!list_allowed_file_extentions.Contains(Path.GetExtension(request.File[i].FileName).ToLower()))
                    {
                        ModelState.AddModelError("", Messages.BOARD_ALLOWED_FILE_EXTENSIONS);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
                        PrepareViewModel(request);
                        return View(request);
                    }
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
                        status = BoardStatus.UPCOMING,
                        title = request.title,
                        content = request.content,
                        update_user = user_id,
                        update_date = now,
                        applicant_cd = request.applicant_cd
                    };

                    var comment_no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                    var comment = new T_BOARDCOMMENT
                    {
                        board_no = board_no,
                        comment_no = comment_no,
                        message = Board_comment.CREATE,
                        update_user = user_id,
                        update_date = now
                    };

                    _context.Add(model);
                    _context.Add(comment);

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
        public IActionResult Update(int id)
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
                var list_allowed_file_extentions = new List<string>() { ".pdf" };
                for (int i = 0; i < request.File.Count; i++)
                {
                    if (!list_allowed_file_extentions.Contains(Path.GetExtension(request.File[i].FileName).ToLower()))
                    {
                        ModelState.AddModelError("", Messages.BOARD_ALLOWED_FILE_EXTENSIONS);
                        ResetWorkDir(DIC_KB_700_DIRECTORY.BOARD, request.work_dir);
                        PrepareViewModel(request);
                        return View(request);
                    }
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
                model.update_date = now;
                model.applicant_cd = request.applicant_cd;

                _context.T_BOARD.Update(model);

                var comment_no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                var comment = new T_BOARDCOMMENT
                {
                    board_no = request.board_no,
                    comment_no = comment_no,
                    message = Board_comment.UPDATE,
                    update_user = user_id,
                    update_date = now
                };
                _context.T_BOARDCOMMENT.Add(comment);

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
                            System.IO.File.Delete(Path.ChangeExtension(filepath, "jpeg"));
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

        [HttpPost]
        public IActionResult AddComment(int board_no, string message)
        {
            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var no = GetNextNo(DataTypes.BOARD_COMMENT_NO);
                
                var model = new T_BOARDCOMMENT
                {
                    board_no = board_no,
                    comment_no = no,
                    message = message,
                    update_user = user_id,
                    update_date = DateTime.Now
                };

                _context.Add(model);

                _context.SaveChanges();
                tran.Commit();

                return Json(new { model.comment_no, update_date = model.update_date.ToString("yyyy年M月d日 H時m分") });
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
                                       register_date = c.update_date.ToString("yyyy年M月d日 H時m分")
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

                var model_files = _context.T_BOARD_FILE.Where(x => x.board_no == board_no).ToList();
                if (model_files != null && model_files.Count > 0)
                {
                    _context.T_BOARD_FILE.RemoveRange(model_files);
                    string dir_main = Path.Combine(_uploadPath, board_no.ToString());
                    Directory.Delete(dir_main, true);
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

        //[HttpGet]
        //public async Task<IActionResult> DownloadFile(int file_no)
        //{
        //    try
        //    {
        //        var model_file = await _context.T_BOARD_FILE.FirstOrDefaultAsync(x => x.file_no == file_no);
        //        if (model_file != null)
        //        {
        //            model_file.filepath = Path.ChangeExtension(model_file.filepath, ".jpeg");
        //            var model = new HomeShowImageViewModel
        //            {
        //                Path = model_file.filepath
        //            };
        //            return RedirectToAction("ShowImage", "Home", model);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
        //        _logger.LogError(ex.StackTrace);
        //    }
        //    return BadRequest();
        //}

        private BoardViewModel CreateViewModel()
        {
            try
            {
                var boardList = (from b in _context.T_BOARD
                                 let v = Convert.ToInt32(b.update_user)
                                 let v1 = b.applicant_cd
                                 select new BoardModel
                                 {
                                     board_no = b.board_no,
                                     status = b.status,
                                     title = b.title,
                                     content = b.content,
                                     registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == v).staf_name,
                                     register_date = b.update_date.ToString("yyyy年M月d日"),
                                     applicant_cd = b.applicant_cd,
                                     applicant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == v1).staf_name
                                 }).ToList();
                var model = new BoardViewModel
                {
                    BoardList = boardList,
                    staffList = _context.M_STAFF
                        .Where(x => x.retired != 1)
                        .Select(u => new BoardViewModelStaff
                        {
                            staff_cd = u.staf_cd,
                            staff_name = u.staf_name
                        })
                        .ToList()
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

        private BoardDetailViewModel? GetDetailView(int board_no)
        {
            try
            {
                var user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);
                var model = (from b in _context.T_BOARD
                             where b.board_no == board_no
                             select new BoardDetailViewModel
                             {
                                 board_no = b.board_no,
                                 status = b.status,
                                 title = b.title,
                                 content = b.content,
                                 registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == user_id).staf_name,
                                 register_date = b.update_date.ToString("yyyy年M月d日"),
                                 notifier_cd = b.notifier_cd,
                                 notifier_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == b.notifier_cd).staf_name,
                                 applicant_cd = b.applicant_cd ?? 0,
                                 staffList = _context.M_STAFF
                                     .Where(x => x.retired != 1)
                                     .Select(u => new BoardViewModelStaff
                                     {
                                         staff_cd = u.staf_cd,
                                         staff_name = u.staf_name
                                     })
                                    .ToList()
                             }).FirstOrDefault();

                model.fileModel.fileList = _context.T_BOARD_FILE.Where(x => x.board_no == board_no).ToList();

                var commentList = (from c in _context.T_BOARDCOMMENT
                                   where c.board_no == board_no
                                   orderby c.update_date ascending
                                   let v = Convert.ToInt32(c.update_user)
                                   select new BoardCommentModel
                                   {
                                       board_no = c.board_no,
                                       comment_no = c.comment_no,
                                       message = c.message,
                                       registrant_cd = v,
                                       registrant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == v).staf_name,
                                       register_date = c.update_date.ToString("yyyy年M月d日 H時m分")
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
                model.staffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new BoardViewModelStaff
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

                    T_BOARD_FILE record_file = new()
                    {
                        board_no = board_no,
                        file_no = GetNextNo(DataTypes.FILE_NO),
                        //filepath = Path.Combine(dir_main, file_name),
                        filepath = Path.Combine(board_no.ToString(), file_name),
                        filename = file_name
                    };
                    await _context.T_BOARD_FILE.AddAsync(record_file);

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