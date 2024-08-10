using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Azure.Core;
using web_groupware.Utilities;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618, CS8629

namespace web_groupware.Controllers
{
    [Authorize]
    public class MemoController : BaseController
    {
        private const int MaxContentLength = 255;

        public MemoController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
        }

        [HttpGet]
        public IActionResult Memo_all(int state = 0, int user = 0, int personal_state = 0, string? keyword = null)
        {
            try
            {
                MemoViewModel model = CreateMemoViewModel(state, user, personal_state, keyword, MemoCategory.ALL);
                TempData["category"] = MemoCategory.ALL;
                TempData["view_mode"] = "all";
                ViewBag.ViewMode = "Memo_all";
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
        public IActionResult Memo_sent(int state = 0, int user = 0, int personal_state = 0, string? keyword = null)
        {
            try
            {
                MemoViewModel model = CreateMemoViewModel(state, user, personal_state, keyword, MemoCategory.SENT);
                TempData["category"] = MemoCategory.SENT;
                TempData["view_mode"] = "sent";
                ViewBag.ViewMode = "Memo_sent";
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
        public IActionResult Memo_received(int state = 0, int user = 0, int personal_state = 0, string? keyword = null)
        {
            try
            {
                MemoViewModel model = CreateMemoViewModel(state, user, personal_state, keyword, MemoCategory.RECEIVED);
                TempData["category"] = MemoCategory.RECEIVED;
                TempData["view_mode"] = "received";
                ViewBag.ViewMode = "Memo_received";
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
        public IActionResult Index(int category, int filter_state, int filter_user, int filter_personal_state, string filter_keyword)
        {
            TempData["filter_state"] = filter_state;
            TempData["filter_user"] = filter_user;
            TempData["filter_personal_state"] = filter_personal_state;
            TempData["filter_keyword"] = filter_keyword;
            TempData["category"] = category;

            var routeValues = new { state = filter_state, user = filter_user, personal_state = filter_personal_state, keyword = filter_keyword };
            if (category == MemoCategory.SENT)
                return RedirectToAction("Memo_sent", routeValues);
            else if (category == MemoCategory.RECEIVED)
                return RedirectToAction("Memo_received", routeValues);
            else
                return RedirectToAction("Memo_all", routeValues);
        }

        public MemoViewModel CreateMemoViewModel(int selected_state = 0, int selected_user = 0, int selected_pesonal_state = 0, string? keyword = null, int category = MemoCategory.ALL)
        {
            try
            {
                var model = new MemoViewModel
                {
                    selectedState = selected_state,
                    selectedPersonalState = selected_pesonal_state,
                    selectedUser = selected_user,
                    category = category,
                    keyword = keyword,

                    staffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new MemoViewModelStaff
                    {
                        staff_cd = u.staf_cd,
                        staff_name = u.staf_name
                    })
                    .ToList(),
                    groupList = _context.M_GROUP
                    .Select(g => new MemoViewModelGroup
                    {
                        group_cd = g.group_cd,
                        group_name = g.group_name
                    })
                    .ToList()
                };
                var comments = _context.M_DIC
                    .Where(m => m.dic_kb == 710)
                    .ToList();
                foreach (var item in comments)
                {
                    model.commentList.Add(new MemoComment
                    {
                        comment_no = item.dic_cd,
                        comment = item.content
                    });
                }

                int user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);

                List<T_MEMO>? memoList = null;
                if (keyword != null && keyword != "")
                {
                    var keyword_ = keyword.ToLower();
                    memoList = (from m in _context.T_MEMO
                                where m.phone.ToLower().Contains(keyword_) || m.content.ToLower().Contains(keyword_)
                                select m).ToList();
                }
                else
                {
                    memoList = (from m in _context.T_MEMO select m).ToList();
                }

                if (selected_pesonal_state != 0)
                {
                    bool check_personal_state = selected_pesonal_state == 2 ? true : false;

                    memoList = memoList
                        .Where(memo =>
                        {
                            var memoPersonalRead = _context.T_INFO_PERSONAL
                                .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                                .Where(m => m.first_no == memo.memo_no && m.staf_cd == user_id)
                                .Where(m => m.already_read == check_personal_state)
                                .FirstOrDefault();

                            return memoPersonalRead != null;
                        })
                        .ToList();
                }

                if (selected_state == 3)
                {
                    memoList = memoList
                        .Where(memo =>
                        {
                            var memoChecked = _context.T_CHECKED
                                .Where(x => x.parent_id == CHECK_PARENT_ID.T_MEMO)
                                .Where(x => x.first_no == memo.memo_no && x.staf_cd == memo.receiver_cd)
                                .FirstOrDefault();

                            return memoChecked != null;
                        })
                        .ToList();
                    selected_state = 0;
                }

                if (memoList != null)
                {
                    for (var i = memoList.Count - 1; i >= 0; i--)
                    {
                        var memo = memoList[i];
                        var memoRead = _context.T_INFO_PERSONAL
                                .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                                .Where(m => m.first_no == memo.memo_no && m.staf_cd == memo.receiver_cd)
                                .Where(m => m.already_read)
                                .FirstOrDefault();
                        int state = memoRead != null ? 1 : 0;
                        if (selected_state == 0 || state == selected_state - 1)
                        {
                            bool is_show = false;
                            if (category == MemoCategory.SENT || category == MemoCategory.ALL)
                            {
                                if (memo.sender_cd == user_id || category == MemoCategory.ALL)
                                {
                                    if (selected_user == 0) is_show = true;
                                    else if (selected_user == 1 && memo.receiver_type == 0 && memo.receiver_cd == user_id) is_show = true;
                                    else if (selected_user > 1 && memo.receiver_type == 1)
                                    {
                                        if (memo.receiver_cd == selected_user - 2) is_show = true;
                                    }
                                }
                            }
                            else if (category == MemoCategory.RECEIVED)
                            {
                                if ((selected_user == 0 || selected_user == 1) && memo.receiver_type == 0 && memo.receiver_cd == user_id) is_show = true;
                                else if (memo.receiver_type == 1 && (selected_user == 0 || selected_user > 1 && memo.receiver_cd == selected_user - 2))
                                {
                                    var memoReader = _context.T_INFO_PERSONAL
                                        .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                                        .Where(m => m.first_no == memo.memo_no && m.staf_cd == user_id)
                                        .FirstOrDefault();
                                    // グループの対象社員は作成者が宛先を登録・変更したタイミングでグループに属していた社員
                                    if (memoReader != null)
                                    {
                                        is_show = model.groupList
                                            .Where(m => m.group_cd == memo.receiver_cd)
                                            .ToList().Any();
                                    }
                                }
                            }

                            if (is_show)
                            {
                                var receiver_name = "";
                                if (memo.receiver_type == 0)
                                {
                                    var user = model.staffList.FirstOrDefault(u => u.staff_cd == memo.receiver_cd);
                                    receiver_name = user.staff_name;
                                }
                                else
                                {
                                    var group = model.groupList.FirstOrDefault(u => u.group_cd == memo.receiver_cd);
                                    receiver_name = group.group_name;
                                }
                                var memoChecked = _context.T_CHECKED
                                    .Where(x => x.parent_id == CHECK_PARENT_ID.T_MEMO)
                                    .Where(x => x.first_no == memo.memo_no && x.staf_cd == user_id)
                                    .FirstOrDefault();

                                model.memoList.Add(new MemoModel
                                {
                                    memo_no = memo.memo_no,
                                    create_date = memo.create_date.ToString("yyyy年M月d日 H時m分"),
                                    state = state,
                                    receiver_type = memo.receiver_type,
                                    receiver_cd = memo.receiver_cd,
                                    receiver_name = receiver_name,
                                    applicant_type = memo.applicant_type,
                                    applicant_cd = memo.applicant_cd,
                                    applicant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == memo.applicant_cd)?.staf_name,
                                    comment_no = memo.comment_no,
                                    phone = memo.phone,
                                    content = memo.content,
                                    already_checked = memoChecked != null
                                });
                            }
                        }
                    }
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

        [HttpGet]
        public IActionResult CreateAll()
        {
            TempData["view_mode"] = "all";
            ViewBag.ViewMode = "Memo_all";

            var viewModel = new MemoDetailViewModel();
            PrepareViewModel(viewModel);
            return View("Create", viewModel);
        }

        [HttpGet]
        public IActionResult CreateSent()
        {
            TempData["view_mode"] = "sent";
            ViewBag.ViewMode = "Memo_sent";

            var viewModel = new MemoDetailViewModel();
            PrepareViewModel(viewModel);
            return View("Create", viewModel);
        }

        [HttpGet]
        public IActionResult CreateReceived()
        {
            TempData["view_mode"] = "received";
            ViewBag.ViewMode = "Memo_received";

            var viewModel = new MemoDetailViewModel();
            PrepareViewModel(viewModel);
            return View("Create", viewModel);
        }

        [HttpGet]
        public IActionResult Create(string viewName)
        {
            var viewModel = new MemoDetailViewModel();
            PrepareViewModel(viewModel);

            return View(viewName, viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MemoDetailViewModel request)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                PrepareViewModel(request);

                return View("Create", request);
            }
            if (request.receiver_cd == 0)
            {
                ModelState.AddModelError("", "宛先は必修項目です。");
                PrepareViewModel(request);

                return View("Create", request);
            }
            if (!IsValidMemoRecord(request, out var message))
            {
                ModelState.AddModelError("", message);
                PrepareViewModel(request);

                return View("Create", request);
            }
            if (_context.T_MEMO == null)
            {
                return Problem("Entity set 'web_groupwareContext.Memo'  is null.");
            }

            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
                var now = DateTime.Now;
                try
                {
                    var memo_no = GetNextNo(DataTypes.MEMO_NO);
                    var record_new = new T_MEMO
                    {
                        memo_no = memo_no,
                        state = 0,
                        receiver_type = request.receiver_type,
                        receiver_cd = request.receiver_cd,
                        applicant_type = request.applicant_type,
                        applicant_cd = request.applicant_cd,
                        comment_no = request.comment_no,
                        phone = request.phone,
                        content = request.content,
                        sender_cd = Convert.ToInt32(user_id),
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.T_MEMO.Add(record_new);

                    string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("Edit", "Memo", new { id = memo_no })}";
                    if (request.receiver_type == 0)
                    {
                        var memo_read = new T_INFO_PERSONAL
                        {
                            info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO),
                            parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                            first_no = memo_no,
                            second_no = 0,
                            third_no = 0,
                            staf_cd = request.receiver_cd,
                            already_read = false,
                            title = "伝言・電話メモ",
                            content = request.content,
                            url = url,
                            added = false,
                            create_user = user_id,
                            create_date = now,
                            update_user = user_id,
                            update_date = now
                        };
                        _context.T_INFO_PERSONAL.Add(memo_read);
                    }
                    else
                    {
                        var staf_cds = _context.T_GROUPSTAFF
                            .Where(m => m.group_cd == request.receiver_cd)
                            .Select(m => m.staf_cd)
                            .ToList();
                        foreach (var staf_cd in staf_cds)
                        {
                            var memo_read = new T_INFO_PERSONAL
                            {
                                info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO),
                                parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                                first_no = memo_no,
                                second_no = 0,
                                third_no = 0,
                                staf_cd = staf_cd,
                                already_read = false,
                                title = "伝言・電話メモ",
                                content = request.content,
                                url = url,
                                added = false,
                                create_user = user_id,
                                create_date = now,
                                update_user = user_id,
                                update_date = now
                            };
                            _context.T_INFO_PERSONAL.Add(memo_read);
                        }
                    }

                    await _context.SaveChangesAsync();

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    throw;
                }
            }
            
            return ProperRedirect();
        }

        [HttpGet]
        public IActionResult EditAll(int memo_no)
        {
            TempData["view_mode"] = "all";
            ViewBag.ViewMode = "Memo_all";
            return Edit(memo_no);

        }

        [HttpGet]
        public IActionResult EditSent(int memo_no)
        {
            TempData["view_mode"] = "sent";
            ViewBag.ViewMode = "Memo_sent";
            return Edit(memo_no);
        }

        [HttpGet]
        public IActionResult EditReceived(int memo_no)
        {
            TempData["view_mode"] = "received";
            ViewBag.ViewMode = "Memo_received";
            return Edit(memo_no);
        }

        [HttpGet]
        public IActionResult Edit(int memo_no)
        {
            var viewModel = GetDetailView(memo_no);
            if (viewModel == null)
            {
                return ProperRedirect();
            }
            return View("Edit", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MemoDetailViewModel request)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_change.FAILURE_001);
                PrepareViewModel(request);

                return View("Edit", request);
            }
            if (!IsValidMemoRecord(request, out var message))
            {
                ModelState.AddModelError("", message);
                PrepareViewModel(request);

                return View("Edit", request);
            }
            if (_context.T_MEMO == null)
            {
                return Problem("Entity set 'web_groupwareContext.Memo'  is null.");
            }

            var memoItem = await _context.T_MEMO.FindAsync(request.memo_no);
            if (memoItem != null)
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;
                        if (request.receiver_type != memoItem.receiver_type || request.receiver_cd != memoItem.receiver_cd)
                        {
                            var itemsRemove = _context.T_INFO_PERSONAL
                                .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                                .Where(m => m.first_no == request.memo_no)
                                .ToList();
                            _context.T_INFO_PERSONAL.RemoveRange(itemsRemove);

                            string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{Url.Action("Edit", "Memo", new { id = request.memo_no })}";
                            if (request.receiver_type == 0)
                            {
                                var memo_read = new T_INFO_PERSONAL
                                {
                                    info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO),
                                    parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                                    first_no = request.memo_no,
                                    second_no = 0,
                                    third_no = 0,
                                    staf_cd = request.receiver_cd,
                                    already_read = false,
                                    title = "伝言・電話メモ",
                                    content = request.content,
                                    url = url,
                                    added = false,
                                    create_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                    create_date = now,
                                    update_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                    update_date = now
                                };
                                _context.T_INFO_PERSONAL.Add(memo_read);
                            }
                            else
                            {
                                var staf_cds = _context.T_GROUPSTAFF
                                    .Where(m => m.group_cd == request.receiver_cd)
                                    .Select(m => m.staf_cd)
                                    .ToList();
                                foreach (var staf_cd in staf_cds)
                                {
                                    var memo_read = new T_INFO_PERSONAL
                                    {
                                        info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO),
                                        parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                                        first_no = request.memo_no,
                                        second_no = 0,
                                        third_no = 0,
                                        staf_cd = staf_cd,
                                        already_read = false,
                                        title = "伝言・電話メモ",
                                        content = request.content,
                                        url = url,
                                        added = false,
                                        create_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                        create_date = now,
                                        update_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                        update_date = now
                                    };
                                    _context.T_INFO_PERSONAL.Add(memo_read);
                                }
                            }
                            memoItem.receiver_type = request.receiver_type;
                            memoItem.receiver_cd = request.receiver_cd;
                        }
                        memoItem.applicant_type = request.applicant_type;
                        memoItem.applicant_cd = request.applicant_cd;
                        memoItem.comment_no = request.comment_no;
                        memoItem.phone = request.phone;
                        memoItem.content = request.content;

                        var user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);
                        memoItem.state = request.state;                        
                        memoItem.update_date = now;

                        _context.T_MEMO.Update(memoItem);
                        await _context.SaveChangesAsync();

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                        _logger.LogError(ex.StackTrace);
                        tran.Dispose();
                        throw;
                    }
                }
            }
  
            return ProperRedirect();
        }

        [HttpGet]
        public IActionResult DeleteAll(int memo_no)
        {
            TempData["view_mode"] = "all";
            ViewBag.ViewMode = "Memo_all";
            return Delete(memo_no);
        }

        [HttpGet]
        public IActionResult DeleteSent(int memo_no)
        {
            TempData["view_mode"] = "sent";
            ViewBag.ViewMode = "Memo_sent";
            return Delete(memo_no);
        }

        [HttpGet]
        public IActionResult DeleteReceived(int memo_no)
        {
            TempData["view_mode"] = "received";
            ViewBag.ViewMode = "Memo_received";
            return Delete(memo_no);
        }

        [HttpGet]
        public IActionResult Delete(int memo_no)
        {
            var viewModel = GetDetailView(memo_no);
            if (viewModel == null)
            {
                return ProperRedirect();
            }
            return View("Delete", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(MemoDetailViewModel request)
        {
            if (_context.T_MEMO == null)
            {
                return Problem("Entity set 'web_groupwareContext.memoItem'  is null.");
            }
            var memoDetail = await _context.T_MEMO.FindAsync(request.memo_no);
            if (memoDetail != null)
            {
                try
                {
                    var itemsRemove = _context.T_INFO_PERSONAL
                        .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                        .Where(m => m.first_no == request.memo_no);
                    _context.T_INFO_PERSONAL.RemoveRange(itemsRemove);

                    var checkedList = _context.T_CHECKED
                        .Where(m => m.parent_id == CHECK_PARENT_ID.T_MEMO)
                        .Where(m => m.first_no == request.memo_no).ToList();
                    _context.T_CHECKED.RemoveRange(checkedList);

                    _context.T_MEMO.Remove(memoDetail);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                    _logger.LogError(ex.StackTrace);
                    throw;
                }
            }

            return ProperRedirect();
        }

        [HttpGet]
        public IActionResult DetailAll(int memo_no)
        {
            TempData["view_mode"] = "all";
            ViewBag.ViewMode = "Memo_all";
            return Detail(memo_no);

        }

        [HttpGet]
        public IActionResult DetailSent(int memo_no)
        {
            TempData["view_mode"] = "sent";
            ViewBag.ViewMode = "Memo_sent";
            return Detail(memo_no);
        }

        [HttpGet]
        public IActionResult DetailReceived(int memo_no)
        {
            TempData["view_mode"] = "received";
            ViewBag.ViewMode = "Memo_received";
            return Detail(memo_no);
        }

        [HttpGet]
        public IActionResult Detail(int memo_no)
        {
            var viewModel = GetDetailView(memo_no);
            if (viewModel == null)
            {
                return ProperRedirect();
            }
            return View("Detail", viewModel);
        }

        /// <summary>
        /// T_CHECKED更新 日報
        /// </summary>
        /// <returns></returns>
        public IActionResult Check_comment_main(string memo_no)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var login_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        var btn_text = "";
                        var t_checked_login_user = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO && x.first_no == int.Parse(memo_no) && x.staf_cd == int.Parse(login_user));
                        if (t_checked_login_user == null)
                        {
                            var now = DateTime.Now;
                            var t_checked_new = new T_CHECKED();
                            t_checked_new.check_no = GetNextNo(DataTypes.CHECK_NO);
                            t_checked_new.parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO;
                            t_checked_new.first_no = int.Parse(memo_no);
                            t_checked_new.staf_cd = int.Parse(login_user);
                            t_checked_new.create_user = login_user;
                            t_checked_new.create_date = now;
                            t_checked_new.update_user = login_user;
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
                        var t_checked = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO && x.first_no == int.Parse(memo_no));
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


        [Authorize]
        public async Task<IActionResult> GetMemoReadCount()
        {
            int count;

            var claim = HttpContext.User.FindFirst(ClaimTypes.STAF_CD);
            if (claim != null)
            {
                int staf_cd = int.Parse(claim.Value);
                count = await Task.Run(() => _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                    .Where(m => m.staf_cd == staf_cd && !m.already_read)
                    .Count());
            }
            else
            {
                count = 0;
            }

            var ret = new
            {
                count
            };
            return new JsonResult(ret);
        }

        private void PrepareViewModel(MemoDetailViewModel model)
        {
            try
            {
                model.staffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new MemoViewModelStaff
                    {
                        staff_cd = u.staf_cd,
                        staff_name = u.staf_name
                    })
                    .ToList();
                model.groupList = _context.M_GROUP
                    .Select(g => new MemoViewModelGroup
                    {
                        group_cd = g.group_cd,
                        group_name = g.group_name
                    })
                    .ToList();
                var comments = _context.M_DIC
                    .Where(m => m.dic_kb == 710)
                    .ToList();
                foreach (var item in comments)
                {
                    model.commentList.Add(new MemoComment
                    {
                        comment_no = item.dic_cd,
                        comment = item.content
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private MemoDetailViewModel? GetDetailView(int memo_no)
        {
            var memo = _context.T_MEMO.FirstOrDefault(x => x.memo_no == memo_no);

            if (memo == null)
                return null;

            var model = new MemoDetailViewModel
            {
                memo_no = memo_no,
                state = memo.state,
                receiver_type = memo.receiver_type,
                receiver_cd = memo.receiver_cd,
                applicant_type = memo.applicant_type,
                applicant_cd = memo.applicant_cd,
                comment_no = memo.comment_no,
                phone = memo.phone,
                content = memo.content,
                staffList = _context.M_STAFF
                    .Where(x => x.retired != 1)
                    .Select(u => new MemoViewModelStaff
                    {
                        staff_cd = u.staf_cd,
                        staff_name = u.staf_name
                    })
                    .ToList(),
                groupList = _context.M_GROUP
                    .Select(g => new MemoViewModelGroup
                    {
                        group_cd = g.group_cd,
                        group_name = g.group_name
                    })
                    .ToList()
            };

            var user_id = @User.FindFirst(ClaimTypes.STAF_CD).Value;
            var iUser_id = Convert.ToInt32(user_id);
            var memoRead = _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                    .Where(m => m.first_no == memo_no && m.staf_cd == iUser_id)
                    .FirstOrDefault();

            using IDbContextTransaction tran = _context.Database.BeginTransaction();
            try
            {
                if (memoRead != null)
                {
                    memoRead.already_read = true;
                    _context.T_INFO_PERSONAL.Update(memoRead);
                }
                else
                {
                    var now = DateTime.Now;
                    memoRead = new T_INFO_PERSONAL
                    {
                        info_personal_no = GetNextNo(DataTypes.INFO_PERSONAL_NO),
                        parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                        first_no = memo_no,
                        second_no = 0,
                        third_no = 0,
                        staf_cd = iUser_id,
                        already_read = true,
                        title = "",
                        content = "",
                        url = "",
                        added = false,
                        create_user = user_id,
                        create_date = now,
                        update_user = user_id,
                        update_date = now
                    };
                    _context.T_INFO_PERSONAL.Add(memoRead);
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

            var comments = _context.M_DIC
                .Where(m => m.dic_kb == 710)
                .ToList();
            foreach (var item in comments)
            {
                model.commentList.Add(new MemoComment
                {
                    comment_no = item.dic_cd,
                    comment = item.content
                });
            }
            var memoChecked = _context.T_CHECKED
                .Where(x => x.parent_id == CHECK_PARENT_ID.T_MEMO)
                .Where(x => x.first_no == memo_no && x.staf_cd.ToString() == user_id)
                .FirstOrDefault();

            var list_memo_checked = _context.T_CHECKED.Where(x => x.parent_id == CHECK_PARENT_ID.T_MEMO && x.first_no == memo_no );

            model.already_checked = memoChecked == null ? false : true;
            model.check_count = list_memo_checked.Count() + "名";
            model.list_check_member = list_memo_checked.GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y }).SelectMany(um => um.y.DefaultIfEmpty()).Select(zz => zz.staf_name).ToList();

            return model;
        }

        private IActionResult ProperRedirect()
        {
            var category = MemoCategory.ALL;
            if (TempData.TryGetValue("category", out var res))
            {
                category = Convert.ToInt32(res);
            }
            var filter_state = 0;
            if (TempData.TryGetValue("filter_state", out res))
            {
                filter_state = Convert.ToInt32(res);
            }
            var filter_user = 0;
            if (TempData.TryGetValue("filter_user", out res))
            {
                filter_user = Convert.ToInt32(res);
            }

            var routeValues = new { state = filter_state, user = filter_user };
            if (category == MemoCategory.SENT)
                return RedirectToAction("Memo_sent", routeValues);
            else if (category == MemoCategory.RECEIVED)
                return RedirectToAction("Memo_received", routeValues);
            else
                return RedirectToAction("Memo_all", routeValues);
        }

        private bool IsValidMemoRecord(MemoDetailViewModel request, out string validationMessage)
        {
            validationMessage = string.Empty;

            Regex regex = new Regex(RegularExpression.TEL);
            if (!string.IsNullOrWhiteSpace(request.phone) && !regex.IsMatch(request.phone))
            {
                validationMessage = "電話番号は半角数字と半角ハイフンのみ入力可能です。";
            }
            else if (request.comment_no == "")
            {
                validationMessage = "用件は必修項目です。";
            }
            else if (string.IsNullOrWhiteSpace(request.content))
            {
                validationMessage = "伝言は必修項目です。";
            }
            else if (request.content.Length > MaxContentLength)
            {
                validationMessage = $"伝言は{MaxContentLength}文字以内で入力してください。";
            }

            return string.IsNullOrEmpty(validationMessage);
        }
        /*
        private bool CheckAllReadMemo(T_MEMO? memoItem)
        {
            if (memoItem == null) return false;

            if (memoItem.receiver_type == 0)
            {
                var memoRead = _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                    .Where(m => m.first_no == memoItem.memo_no && m.staf_cd == memoItem.receiver_cd && m.already_read)
                    .FirstOrDefault();
                return (memoRead != null);
            }
            else
            {
                var staff_cds = _context.T_GROUPSTAFF
                    .Where(m => m.group_cd == memoItem.receiver_cd)
                    .Select(m => m.staf_cd)
                    .ToList();
                foreach(var staff in staff_cds)
                {
                    var memoRead = _context.T_INFO_PERSONAL
                        .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                        .Where(m => m.first_no == memoItem.memo_no && m.staf_cd == staff && m.already_read)
                        .FirstOrDefault();
                    if (memoRead == null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        */
    }
}