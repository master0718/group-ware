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
        public const int BUKKEN_COMMENT_CD = 2;

        public const int MEMO_STATE_ALL = 0;
        public const int MEMO_STATE_UNREAD = 0;
        public const int MEMO_STATE_READ = 1;
        public const int MEMO_STATE_WORKING = 2;
        public const int MEMO_STATE_FINISH = 3;

        private const int MaxContentLength = 255;

        public MemoController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
        }

        [HttpGet]
        public IActionResult Memo_sent(int state = 0, int user = 0)
        {
            try
            {
                MemoViewModel model = CreateMemoViewModel(state, user, true);
                TempData["is_sent"] = true;
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
        public IActionResult Memo_received(int state = 0, int user = 0)
        {
            try
            {
                MemoViewModel model = CreateMemoViewModel(state, user, false);
                TempData["is_sent"] = false;
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
        public IActionResult Filter(bool is_sent, int filter_state, int filter_user)
        {
            TempData["filter_state"] = filter_state;
            TempData["filter_user"] = filter_user;
            TempData["is_sent"] = is_sent;

            var routeValues = new { state = filter_state, user = filter_user };
            if (is_sent)
                return RedirectToAction("Memo_sent", routeValues);
            else
                return RedirectToAction("Memo_received", routeValues);
        }

        public MemoViewModel CreateMemoViewModel(int selected_state = 0, int selected_user = 0, bool is_sent = true)
        {
            try
            {
                var model = new MemoViewModel
                {
                    selectedState = selected_state,
                    selectedUser = selected_user,
                    isSent = is_sent,

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
                var memoList = _context.T_MEMO.ToList();
                for (var i = memoList.Count - 1; i >= 0; i--)
                {
                    var memo = memoList[i];

                    if (selected_state == 0 || memo.state == selected_state - 1)
                    {
                        bool is_show = false;
                        if (is_sent)
                        {
                            if (memo.sender_cd == user_id)
                            {
                                if (selected_user == 0) is_show = true;
                                else if (selected_user == 1 && memo.receiver_type == 0 && memo.receiver_cd == user_id) is_show = true;
                                else if (selected_user > 1 && memo.receiver_type == 1)
                                {
                                    if (memo.receiver_cd == selected_user - 2) is_show = true;
                                }
                            }

                        }
                        else
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
                            var sender = model.staffList.FirstOrDefault(u => u.staff_cd == memo.sender_cd);
                            var sender_name = sender?.staff_name;

                            var memoReaders = _context.T_INFO_PERSONAL
                                .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                                .Where(m => m.first_no == memo.memo_no && m.already_checked)
                                .Include(m => m.staff)
                                .ToList();
                            var readerNames = "";
                            foreach (var memoReader in memoReaders)
                            {
                                if (readerNames.Length != 0) readerNames += "、";
                                readerNames += memoReader.staff.staf_name;
                            }
                            var working_msg = "";
                            if (memo.working_cd > 0)
                            {
                                var working = _context.M_STAFF.FirstOrDefault(u => u.staf_cd == memo.working_cd);
                                working_msg = memo.working_date.ToString("yyyy年M月d日 H時m分  ") + working?.staf_name;
                            }
                            var finish_msg = "";
                            if (memo.finish_cd > 0)
                            {
                                var working = _context.M_STAFF.FirstOrDefault(u => u.staf_cd == memo.finish_cd);
                                finish_msg = memo.finish_date.ToString("yyyy年M月d日 H時m分  ") + working?.staf_name;
                            }

                            model.memoList.Add(new MemoModel
                            {
                                memo_no = memo.memo_no,
                                create_date = memo.create_date.ToString("yyyy年M月d日 H時m分"),
                                state = memo.state,
                                receiver_type = memo.receiver_type,
                                receiver_cd = memo.receiver_cd,
                                receiver_name = receiver_name,
                                applicant_type = memo.applicant_type,
                                applicant_cd = memo.applicant_cd,
                                applicant_name = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == memo.applicant_cd)?.staf_name,
                                comment_no = memo.comment_no,
                                phone = memo.phone,
                                content = memo.content,
                                sender_name = sender_name,
                                is_editable = memo.sender_cd == user_id,
                                readers = readerNames,
                                working_msg = working_msg,
                                finish_msg = finish_msg
                            });
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
                try
                {
                    var record_new = new T_MEMO
                    {
                        memo_no = GetNextNo(DataTypes.MEMO_NO),
                        state = MEMO_STATE_UNREAD,
                        receiver_type = request.receiver_type,
                        receiver_cd = request.receiver_cd,
                        applicant_type = request.applicant_type,
                        applicant_cd = request.applicant_cd,
                        comment_no = request.comment_no,
                        phone = request.phone,
                        content = request.content,
                        sender_cd = Convert.ToInt32(user_id),
                        working_cd = 0,
                        finish_cd = 0,
                        create_user = user_id,
                        create_date = DateTime.Now,
                        update_user = user_id,
                        update_date = DateTime.Now
                    };
                    var tracked = _context.T_MEMO.Add(record_new);

                    if (request.receiver_type == 0)
                    {
                        var memo_read = new T_INFO_PERSONAL
                        {
                            parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                            first_no = tracked.Entity.memo_no,
                            second_no = 0,
                            third_no = 0,
                            staf_cd = request.receiver_cd,
                            already_checked = false,
                            title = "",
                            content = "",
                            url = "",
                            added = false,
                            create_user = user_id,
                            create_date = DateTime.Now,
                            update_user = user_id,
                            update_date = DateTime.Now
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
                                parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                                first_no = tracked.Entity.memo_no,
                                second_no = 0,
                                third_no = 0,
                                staf_cd = staf_cd,
                                already_checked = false,
                                title = "",
                                content = "",
                                url = "",
                                added = false,
                                create_user = user_id,
                                create_date = DateTime.Now,
                                update_user = user_id,
                                update_date = DateTime.Now
                            };
                            _context.T_INFO_PERSONAL.Add(memo_read);
                        }
                    }

                    await _context.SaveChangesAsync();

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            
            return ProperRedirect();
        }

        public async Task<IActionResult> UpdateReadState(int memo_no)
        {
            if (_context.T_MEMO == null)
            {
                return Problem("Entity set 'web_groupwareContext.Memo'  is null.");
            }

            int user_id = Convert.ToInt32(@User.FindFirst(ClaimTypes.STAF_CD).Value);
            try
            {
                var memoRead = await _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                    .Where(m => m.first_no == memo_no && m.staf_cd == user_id)
                    .FirstOrDefaultAsync();
                if (memoRead != null && !memoRead.already_checked)
                {
                    using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                    {
                        memoRead.already_checked = true;

                        _context.T_INFO_PERSONAL.Update(memoRead);
                        await _context.SaveChangesAsync();

                        var memoItem = await _context.T_MEMO.FindAsync(memo_no);
                        if (memoItem.state == MEMO_STATE_UNREAD && CheckAllReadMemo(memoItem))
                        {
                            memoItem.state = MEMO_STATE_READ;
                            _context.T_MEMO.Update(memoItem);
                            await _context.SaveChangesAsync();
                        }

                        tran.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }

            return ProperRedirect();
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
            var viewModel = getDetailView(memo_no);
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
                        if (request.receiver_type != memoItem.receiver_type || request.receiver_cd != memoItem.receiver_cd)
                        {
                            var itemsRemove = _context.T_INFO_PERSONAL
                                .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                                .Include(m => m.staff)
                                .Where(m => m.first_no == request.memo_no)
                                .ToList();
                            _context.T_INFO_PERSONAL.RemoveRange(itemsRemove);

                            if (request.receiver_type == 0)
                            {
                                var memo_read = new T_INFO_PERSONAL
                                {
                                    parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                                    first_no = request.memo_no,
                                    second_no = 0,
                                    third_no = 0,
                                    staf_cd = request.receiver_cd,
                                    already_checked = false,
                                    title = "",
                                    content = "",
                                    url = "",
                                    added = false,
                                    create_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                    create_date = DateTime.Now,
                                    update_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                    update_date = DateTime.Now
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
                                        parent_id = INFO_PERSONAL_PARENT_ID.T_MEMO,
                                        first_no = request.memo_no,
                                        second_no = 0,
                                        third_no = 0,
                                        staf_cd = staf_cd,
                                        already_checked = false,
                                        title = "",
                                        content = "",
                                        url = "",
                                        added = false,
                                        create_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                        create_date = DateTime.Now,
                                        update_user = @User.FindFirst(ClaimTypes.STAF_CD).Value,
                                        update_date = DateTime.Now
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
                        if (request.working == 1)
                        {
                            if (memoItem.working_cd == 0)
                            {
                                memoItem.working_cd = user_id;
                                memoItem.working_date = DateTime.Now;
                            }
                        }
                        else
                        {
                            memoItem.working_cd = 0;
                        }
                        if (request.finish == 1)
                        {
                            if (memoItem.finish_cd == 0)
                            {
                                memoItem.finish_cd = user_id;
                                memoItem.finish_date = DateTime.Now;
                            }
                        }
                        else
                        {
                            memoItem.finish_cd = 0;
                        }
                        if (request.finish == 1)
                        {
                            memoItem.state = MEMO_STATE_FINISH;
                        }
                        else if (request.working == 1)
                        {
                            memoItem.state = MEMO_STATE_WORKING;
                        }
                        else
                        {
                            memoItem.state = CheckAllReadMemo(memoItem) ? MEMO_STATE_READ : MEMO_STATE_UNREAD;
                        }
                        memoItem.update_date = DateTime.Now;

                        _context.T_MEMO.Update(memoItem);
                        await _context.SaveChangesAsync();

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }
  
            return ProperRedirect();
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
            var viewModel = getDetailView(memo_no);
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

                    _context.T_MEMO.Remove(memoDetail);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            return ProperRedirect();
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
                    .Where(m => m.staf_cd == staf_cd && !m.already_checked)
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
        private MemoDetailViewModel? getDetailView(int memo_no)
        {
            var memo = _context.T_MEMO.FirstOrDefault(x => x.memo_no == memo_no);

            if (memo == null)
                return null;

            var model = new MemoDetailViewModel
            {
                memo_no = memo.memo_no,
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

            var memoReaders = _context.T_INFO_PERSONAL
                .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                .Include(m => m.staff)
                .Where(m => m.first_no == memo_no && m.already_checked)
                .ToList();
            var readerNames = "";
            foreach (var memoReader in memoReaders)
            {
                if (readerNames.Length != 0) readerNames += "、";
                readerNames += memoReader.staff.staf_name;
            }
            model.readers = readerNames;
            if (memo.working_cd > 0)
            {
                var working = _context.M_STAFF.FirstOrDefault(u => u.staf_cd == memo.working_cd)?.staf_name;
                model.working_msg = memo.working_date.ToString("yyyy年MM月dd日 HH時mm分  ") + working;
            }
            if (memo.finish_cd > 0)
            {
                var working = _context.M_STAFF.FirstOrDefault(u => u.staf_cd == memo.finish_cd)?.staf_name;
                model.finish_msg = memo.finish_date.ToString("yyyy年MM月dd日 HH時mm分  ") + working;
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

            return model;
        }

        private IActionResult ProperRedirect()
        {
            bool is_sent = true;
            if (TempData.TryGetValue("is_sent", out var res))
            {
                is_sent = Convert.ToBoolean(res);
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
            if (is_sent)
                return RedirectToAction("Memo_sent", routeValues);
            else
                return RedirectToAction("Memo_received", routeValues);
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
        private bool CheckAllReadMemo(T_MEMO? memoItem)
        {
            if (memoItem == null) return false;

            if (memoItem.receiver_type == 0)
            {
                var memoRead = _context.T_INFO_PERSONAL
                    .Where(m => m.parent_id == INFO_PERSONAL_PARENT_ID.T_MEMO)
                    .Where(m => m.first_no == memoItem.memo_no && m.staf_cd == memoItem.receiver_cd && m.already_checked)
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
                        .Where(m => m.first_no == memoItem.memo_no && m.staf_cd == staff && m.already_checked)
                        .FirstOrDefault();
                    if (memoRead == null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}