using System.Data;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json.Linq;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public ReportController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }
        enum enum_mode
        {
            Create = 1,
            Update,
            Delete,
            Reffer,
            Comment
        }
        #region "日報"

        //[HttpPost]
        public IActionResult Index(ReportViewModel model)
        {
            try
            {
                //初期表示と判断
                if (model.cond_already_read == null)
                {
                    model.cond_date = DateTime.Now.ToString("yyyy/MM/dd");
                    model.cond_already_read = "1";
                }
                model = createViewModel(model.cond_date, model.cond_already_read);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        public ReportViewModel createViewModel(string? date, string cont_already_read_flg)//cont_already_read_flg=1は未読
        {
            try
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine(" SELECT ");
                sql.AppendLine("  	B.report_no ");
                sql.AppendLine("  	,B.update_user ");
                sql.AppendLine("  	,S2.staf_name ");
                sql.AppendLine("  	,B.report_date ");
                sql.AppendLine("  	,B.update_date as update_date_1 ");
                sql.AppendLine("  	,S1.update_date as update_date_2 ");
                sql.AppendLine("  	,B.message  ");
                sql.AppendLine(" FROM T_REPORT B ");

                sql.AppendLine(" LEFT JOIN T_INFO_PERSONAL S3 ");
                sql.AppendFormat(" ON S3.parent_id={0} and S3.first_no= B.report_no and S3.staf_cd = {1}", INFO_PERSONAL_PARENT_ID.T_REPORT, HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value);

                sql.AppendLine(" LEFT JOIN ");
                sql.AppendLine(" ( select report_no,comment_no,update_user,update_date,row_number() over(PARTITION BY report_no ORDER BY update_date DESC) as num from T_REPORTCOMMENT ) S1 ");
                sql.AppendLine(" ON B.report_no = S1.report_no ");

                sql.AppendLine(" LEFT JOIN ");
                sql.AppendFormat(" ( select first_no as report_no from T_INFO_PERSONAL where parent_id={0} and staf_cd={1} and already_read=0 group by first_no ) S4 ", INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT, HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value);
                sql.AppendLine(" ON B.report_no = S4.report_no ");

                sql.AppendLine(" LEFT JOIN M_STAFF S2 ");
                sql.AppendLine(" ON B.update_user = S2.staf_cd ");
                sql.AppendLine(" WHERE 1=1 ");
                sql.AppendLine(" AND (num =1 OR num IS NULL ) ");

                if (date != null)
                {
                    sql.AppendFormat(" AND B.report_date = '{0}' ", date);
                }

                if (cont_already_read_flg == "1")
                {
                    sql.AppendFormat(" AND (S3.already_read = 0 or S4.report_no is not null) ");
                }
                sql.AppendLine(" ORDER BY report_date DESC, update_date_1 DESC ");

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = sql.ToString();
                        var dataAdapter = new SqlDataAdapter(cmd);
                        dataAdapter.Fill(dt);
                    }
                }

                ReportViewModel model = new ReportViewModel();
                model.cond_date = date;
                model.cond_already_read = cont_already_read_flg;
                var compare_day = "";
                foreach (DataRow dr in dt.Rows)
                {
                    var staf_cd = int.Parse(HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value);
                    var t_report_read = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == dr.Field<int>("report_no") && x.staf_cd == staf_cd && x.already_read == false);
                    var records = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.first_no == dr.Field<int>("report_no") && x.staf_cd == staf_cd && x.already_read == false);
                    string count = t_report_read.Count() + records.Count() == 0 ? "" : (t_report_read.Count() + records.Count()).ToString();



                    string update_date = "";
                    if (dr["update_date_2"] == DBNull.Value)
                    {
                        update_date = dr.Field<DateTime>("update_date_1").ToString("yyyy/M/d HH:mm");
                    }
                    else
                    {
                        update_date = dr.Field<DateTime>("update_date_2").ToString("yyyy/M/d HH:mm");
                    }
                    if (compare_day != dr.Field<DateTime>("report_date").ToString("D"))
                    {
                        compare_day = dr.Field<DateTime>("report_date").ToString("D");
                        model.list_report.Add(new List<Report>());
                    }
                    model.list_report[model.list_report.Count - 1].Add(new Report
                    {
                        report_no = dr.Field<int>("report_no"),
                        name = dr.Field<string>("staf_name"),
                        report_date = dr.Field<DateTime>("report_date").ToString("M/d"),
                        update_date = update_date,
                        message = dr.Field<string>("message"),
                        count = count,
                    });

                }
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        #endregion
        #region "日報詳細"
        [HttpGet]
        public IActionResult ReportDetail(int mode, int report_no, DateTime? cond_date, string cond_already_read)//cond_dateは登録後に日報画面に戻った時のために日付保持
        {
            ReportDetailViewModel model = new ReportDetailViewModel();
            try
            {
                if (mode == 1)
                {
                    model.mode = (int)enum_mode.Create;
                    model.report_date = DateTime.Now;
                    //SetSelectListItem(model);
                }
                else
                {
                    model = createDetailViewModel(mode, report_no);
                    Read_report(model);
                    //Update_already_checked(report_no);
                }
                model.cond_date = cond_date?.ToString("yyyy/MM/dd");
                model.cond_already_read = cond_already_read;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
            return View("ReportDetail", model);
        }

        public ReportDetailViewModel createDetailViewModel(int mode, int report_no)
        {
            try
            {
                var staf_cd = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                ReportDetailViewModel model = new ReportDetailViewModel();
                model.mode = mode;
                DataTable dt = new DataTable();
                StringBuilder sql = new StringBuilder();
                sql.AppendLine(" SELECT ");
                sql.AppendLine("  	B.report_no ");
                sql.AppendLine("  	,B.report_date ");
                sql.AppendLine("  	,B.message ");
                sql.AppendLine("  	,B.update_user ");
                sql.AppendLine("  	,B.update_date ");
                sql.AppendLine("  	,B.create_user ");
                sql.AppendLine("  	,B.create_date ");
                sql.AppendLine(" FROM T_REPORT B ");
                sql.AppendLine(" WHERE 1=1 ");
                sql.AppendFormat(" AND B.report_no = {0} ", report_no);

                using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = sql.ToString();
                        var dataAdapter = new SqlDataAdapter(cmd);
                        dataAdapter.Fill(dt);
                    }
                }
                model.report_no = dt.Rows[0].Field<int>("report_no");
                model.report_date = dt.Rows[0].Field<DateTime>("report_date");
                model.message = dt.Rows[0].Field<string>("message") ?? "";
                model.create_user = dt.Rows[0].Field<string>("create_user");
                model.update_user = dt.Rows[0].Field<string>("update_user");
                model.update_date = dt.Rows[0].Field<DateTime>("update_date").ToString("yyyy/MM/dd HH:mm");
                model.create_user = dt.Rows[0].Field<string>("create_user");
                model.create_date = dt.Rows[0].Field<DateTime>("create_date").ToString("yyyy/MM/dd HH:mm");
                model.isMe = dt.Rows[0].Field<string>("create_user") == staf_cd ? true : false;
                var t_checked_main = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == dt.Rows[0].Field<int>("report_no")  && x.staf_cd.ToString() == HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                var list_t_checked_main = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == dt.Rows[0].Field<int>("report_no") );

                model.already_checked = t_checked_main == null ? false : true;
                model.check_count = list_t_checked_main.Count() + "名";
                model.list_check_member = list_t_checked_main.GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y }).SelectMany(um => um.y.DefaultIfEmpty()).Select(zz => zz.staf_name).ToList();


                //model.list_selected_staf_cd_report = _context.T_REPORT_READ.Where(x => x.report_no == dt.Rows[0].Field<int>("report_no")).Select(x => x.staf_cd.ToString()).ToList();
                //SetSelectListItem(model);
                var t_report_checked = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == dt.Rows[0].Field<int>("report_no") && x.staf_cd.ToString() == staf_cd);

                //var t_report_read = _context.T_REPORT_READ.FirstOrDefault(x => x.report_no == dt.Rows[0].Field<int>("report_no") && x.staf_cd.ToString() == HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                model.already_checked = t_report_checked == null ? false : true;
                //コメント表示欄
                if (mode == (int)enum_mode.Update || mode == (int)enum_mode.Delete || mode == (int)enum_mode.Reffer || mode == (int)enum_mode.Comment)
                {
                    dt = new DataTable();
                    sql.Clear();
                    sql.AppendLine(" SELECT ");
                    sql.AppendLine("  	B.report_no ");
                    sql.AppendLine("  	,S1.comment_no ");
                    sql.AppendLine("  	,S1.update_date ");
                    sql.AppendLine("  	,S1.message ");
                    sql.AppendLine("  	,S2.staf_name ");
                    sql.AppendLine(" FROM T_REPORT B ");
                    sql.AppendLine(" INNER JOIN T_REPORTCOMMENT S1 ");
                    sql.AppendLine(" ON B.report_no = S1.report_no ");
                    sql.AppendLine(" LEFT JOIN M_STAFF S2 ");
                    sql.AppendLine(" ON S1.update_user = S2.staf_cd ");
                    sql.AppendLine(" WHERE 1=1 ");
                    sql.AppendFormat(" AND B.report_no = {0} ", report_no);
                    sql.AppendLine(" ORDER BY S1.update_date DESC ");

                    using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                    {
                        con.Open();
                        using (SqlCommand cmd = con.CreateCommand())
                        {
                            cmd.CommandText = sql.ToString();
                            var dataAdapter = new SqlDataAdapter(cmd);
                            dataAdapter.Fill(dt);
                        }
                    }
                    model.list_report = new List<CommentDetail>();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var t_checked = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.first_no == dt.Rows[i].Field<int>("report_no") && x.second_no == dt.Rows[i].Field<int>("comment_no") && x.staf_cd.ToString() == HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                        var list_t_checked = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.first_no == dt.Rows[i].Field<int>("report_no") && x.second_no == dt.Rows[i].Field<int>("comment_no"));
                        model.list_report.Add(new CommentDetail()
                        {
                            comment_no = dt.Rows[i].Field<int>("comment_no"),
                            update_user = dt.Rows[i].Field<string>("staf_name"),
                            update_date = dt.Rows[i].Field<DateTime>("update_date").ToString("yyyy/MM/dd HH:mm"),
                            message = dt.Rows[i].Field<string>("message"),
                            already_checked_comment = t_checked == null ?false:true,
                            check_count = list_t_checked.Count() + "名",
                            list_check_member = list_t_checked.GroupJoin(_context.M_STAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new { x, y }).SelectMany(um => um.y.DefaultIfEmpty()).Select(zz => zz.staf_name).ToList(),

                        }
                            );
                    }
                }
                //コメント入力欄
                if (mode == (int)enum_mode.Comment)
                {
                    model.report = new CommentDetail();
                }
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_register.FAILURE_001);
                //SetSelectListItem(model);
                return View("ReportDetail", model);
            }
            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var next_no = GetNextNo(DataTypes.REPORT_NO);
                    var now = DateTime.Now;
                    var recoard_new = new T_REPORT();
                    recoard_new.report_no = next_no;
                    recoard_new.report_date = model.report_date;
                    recoard_new.message = model.message;
                    recoard_new.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    recoard_new.create_date = now;
                    recoard_new.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    recoard_new.update_date = now;
                    _context.T_REPORT.Add(recoard_new);
                    _context.SaveChanges();

                    var t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd.ToString() != HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value).ToListAsync();
                    for (int i = 0; i < t_staffm.Count; i++)
                    {
                        var recoard_comment_new = new T_INFO_PERSONAL();
                        recoard_comment_new.info_personal_no = GetNextNo(Utilities.DataTypes.INFO_PERSONAL_NO);
                        recoard_comment_new.parent_id = INFO_PERSONAL_PARENT_ID.T_REPORT;
                        recoard_comment_new.first_no = next_no;
                        recoard_comment_new.staf_cd = t_staffm[i].staf_cd;
                        recoard_comment_new.title = "日報";
                        recoard_comment_new.content = model.message;
                        recoard_comment_new.url = string.Format("{0}://{1}{2}{3}/ReportDetail?mode=4&report_no={4}", HttpContext.Request.Scheme, HttpContext.Request.Host, HttpContext.Request.PathBase + "/", ControllerContext.ActionDescriptor.ControllerName, next_no);
                        recoard_comment_new.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        recoard_comment_new.create_date = now;
                        recoard_comment_new.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        recoard_comment_new.update_date = now;
                        _context.T_INFO_PERSONAL.Add(recoard_comment_new);
                    }
                    _context.SaveChanges();

                    tran.Commit();

                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    //SetSelectListItem(model);
                    return View("ReportDetail", model);
                }
            }
            return RedirectToAction("Index", "Report", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ReportDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_register.FAILURE_001);
                //SetSelectListItem(model);
                return View("ReportDetail", model);
            }
            var recoard = _context.T_REPORT.FirstOrDefault(x => x.report_no == model.report_no);
            if (recoard.update_user != HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value)
            {
                ModelState.AddModelError("", "投稿者以外は編集できません。");
                //SetSelectListItem(model);
                return View("ReportDetail", model);
            }

            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var now = DateTime.Now;

                    recoard.message = model.message;
                    recoard.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    recoard.update_date = now;

                    var t_report_read = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == model.report_no);
                    _context.RemoveRange(t_report_read);
                    _context.SaveChanges();

                    var t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd.ToString() != HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value).ToListAsync();
                    for (int i = 0; i < t_staffm.Count; i++)
                    {
                        var recoard_comment_new = new T_INFO_PERSONAL();
                        recoard_comment_new.info_personal_no = GetNextNo(Utilities.DataTypes.INFO_PERSONAL_NO);
                        recoard_comment_new.parent_id = INFO_PERSONAL_PARENT_ID.T_REPORT;
                        recoard_comment_new.first_no = (int)model.report_no;
                        recoard_comment_new.staf_cd = t_staffm[i].staf_cd;
                        recoard_comment_new.title = "日報";
                        recoard_comment_new.content = model.message;
                        recoard_comment_new.url = string.Format("{0}://{1}{2}{3}/ReportDetail?mode=4&report_no={4}", HttpContext.Request.Scheme, HttpContext.Request.Host, HttpContext.Request.PathBase + "/", ControllerContext.ActionDescriptor.ControllerName, model.report_no);
                        recoard_comment_new.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        recoard_comment_new.create_date = now;
                        recoard_comment_new.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        recoard_comment_new.update_date = now;
                        _context.T_INFO_PERSONAL.Add(recoard_comment_new);
                    }
                    _context.SaveChanges();

                    tran.Commit();

                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    //SetSelectListItem(model);
                    return View("ReportDetail", model);
                }
            }
            model.mode = 4;
            return RedirectToAction("ReportDetail", "Report", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(ReportDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_register.FAILURE_001);
                //SetSelectListItem(model);
                return View("ReportDetail", model);
            }
            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var recoard = _context.T_REPORT.FirstOrDefault(x => x.report_no == model.report_no);
                    _context.T_REPORT.Remove(recoard);
                    _context.SaveChanges();
                    var recoard_read = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == model.report_no);
                    if (recoard_read.Count() > 0)
                    {
                        _context.T_INFO_PERSONAL.RemoveRange(recoard_read);
                        _context.SaveChanges();
                    }

                    var recoard_comment = _context.T_REPORTCOMMENT.Where(x => x.report_no == model.report_no);
                    if (recoard_comment.Count() > 0)
                    {
                        _context.T_REPORTCOMMENT.RemoveRange(recoard_comment);
                        _context.SaveChanges();
                    }
                    var recoard_comment_read = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.first_no == model.report_no);
                    if (recoard_comment_read.Count() > 0)
                    {
                        _context.T_INFO_PERSONAL.RemoveRange(recoard_comment_read);
                        _context.SaveChanges();
                    }

                    tran.Commit();

                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    //SetSelectListItem(model);
                    return View("ReportDetail", model);
                }
            }
            return RedirectToAction("Index", "Report", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create_Comment(ReportDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_register.FAILURE_001);
                //SetSelectListItem(model);
                return View("ReportDetail", model);
            }
            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var now = DateTime.Now;
                    int nextComment_no = GetNextNo(Utilities.DataTypes.REPORT_COMMENT_NO);

                    var record = new T_REPORTCOMMENT();
                    record.report_no = (int)model.report_no;
                    record.comment_no = nextComment_no;
                    record.message = model.report.message;
                    record.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    record.create_date = now;
                    record.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                    record.update_date = now;
                    _context.T_REPORTCOMMENT.Add(record);
                    _context.SaveChanges();

                    var t_report = _context.T_REPORT.FirstOrDefault(x => x.report_no == model.report_no);
                    if (t_report.update_user != HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value)
                    {
                        var record_read = new T_INFO_PERSONAL();
                        record_read.info_personal_no = GetNextNo(Utilities.DataTypes.INFO_PERSONAL_NO);
                        record_read.parent_id = INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT;
                        record_read.first_no = (int)model.report_no;
                        record_read.second_no = nextComment_no;
                        record_read.staf_cd = int.Parse(t_report.update_user);
                        record_read.title = "日報コメント";
                        record_read.content = model.message;
                        record_read.url = string.Format("{0}://{1}{2}{3}/ReportDetail?mode=4&report_no={4}", HttpContext.Request.Scheme, HttpContext.Request.Host, HttpContext.Request.PathBase + "/", ControllerContext.ActionDescriptor.ControllerName, model.report_no);
                        record_read.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        record_read.create_date = now;
                        record_read.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        record_read.update_date = now;
                        _context.T_INFO_PERSONAL.Add(record_read);
                    }

                    var t_report_read = _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == (int)model.report_no).ToList();
                    for (int i = 0; i < t_report_read.Count(); i++)
                    {
                        if (t_report_read[i].staf_cd.ToString() == HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value) continue;
                        var record_read = new T_INFO_PERSONAL();
                        record_read.info_personal_no = GetNextNo(Utilities.DataTypes.INFO_PERSONAL_NO);
                        record_read.parent_id = INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT;
                        record_read.first_no = (int)model.report_no;
                        record_read.second_no = nextComment_no;
                        record_read.staf_cd = t_report_read[i].staf_cd;
                        record_read.title = "日報コメント";
                        record_read.content = model.message;
                        record_read.url = string.Format("{0}://{1}{2}{3}/ReportDetail?mode=4&report_no={4}", HttpContext.Request.Scheme, HttpContext.Request.Host, HttpContext.Request.PathBase + "/", ControllerContext.ActionDescriptor.ControllerName, model.report_no);
                        record_read.create_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        record_read.create_date = now;
                        record_read.update_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        record_read.update_date = now;
                        _context.T_INFO_PERSONAL.Add(record_read);
                    }

                    _context.SaveChanges();

                    tran.Commit();

                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    //SetSelectListItem(model);
                    return View("ReportDetail", model);
                }
            }
            return RedirectToAction("ReportDetail", "Report", model);
        }
        /// <summary>
        /// T_CHECKED更新 日報
        /// </summary>
        /// <returns></returns>
        public IActionResult Check_comment_main(string report_no)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var login_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        var btn_text = "";
                        var t_checked_login_user = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == int.Parse(report_no) && x.staf_cd == int.Parse(login_user));
                        if (t_checked_login_user == null)
                        {
                            var now = DateTime.Now;
                            var t_checked_new = new T_CHECKED();
                            t_checked_new.check_no = GetNextNo(DataTypes.CHECK_NO);
                            t_checked_new.parent_id = INFO_PERSONAL_PARENT_ID.T_REPORT;
                            t_checked_new.first_no = int.Parse(report_no);
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
                        var t_checked = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.first_no == int.Parse(report_no));
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
        /// T_CHECKED更新　コメント
        /// </summary>
        /// <returns></returns>
        public IActionResult Check_comment(string report_no, string comment_no)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var login_user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        var btn_text = "";
                        var t_checked_login_user = _context.T_CHECKED.FirstOrDefault(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.first_no == int.Parse(report_no) && x.second_no == int.Parse(comment_no) && x.staf_cd == int.Parse(login_user));
                        if (t_checked_login_user == null)
                        {
                            var now = DateTime.Now;
                            var t_checked_new = new T_CHECKED();
                            t_checked_new.check_no = GetNextNo(DataTypes.CHECK_NO);
                            t_checked_new.parent_id = INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT;
                            t_checked_new.first_no = int.Parse(report_no);
                            t_checked_new.second_no = int.Parse(comment_no);
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
                        var t_checked = _context.T_CHECKED.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.first_no == int.Parse(report_no) && x.second_no == int.Parse(comment_no));
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
        /// already_read更新
        /// </summary>
        /// <param name="report_no"></param>
        /// <returns></returns
        public void Read_report(ReportDetailViewModel model)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;
                        var user = HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value;
                        StringBuilder sql = new StringBuilder();
                        //日報
                        sql.AppendLine(" UPDATE ");
                        sql.AppendLine(" T_INFO_PERSONAL ");
                        sql.AppendFormat(" SET already_read=1,update_user='{0}',update_date='{1}'", user, now);
                        sql.AppendLine(" WHERE 1=1 ");
                        sql.AppendFormat(" AND parent_id = {0} ", INFO_PERSONAL_PARENT_ID.T_REPORT);
                        sql.AppendFormat(" AND first_no = {0} ", model.report_no);
                        sql.AppendFormat(" AND staf_cd = {0} ", user);

                        using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                        {
                            con.Open();
                            using (SqlCommand cmd = con.CreateCommand())
                            {
                                cmd.CommandText = sql.ToString();
                                cmd.ExecuteNonQuery();
                            }
                        }
                        sql.Clear();
                        //日報コメント
                        sql.AppendLine(" UPDATE ");
                        sql.AppendLine(" T_INFO_PERSONAL ");
                        sql.AppendFormat(" SET already_read=1,update_user='{0}',update_date='{1}'", user, now);
                        sql.AppendLine(" WHERE 1=1 ");
                        sql.AppendFormat(" AND parent_id = {0} ", INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT);
                        sql.AppendFormat(" AND first_no = {0} ", model.report_no);
                        sql.AppendFormat(" AND staf_cd = {0} ", user);
                        using (SqlConnection con = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                        {
                            con.Open();
                            using (SqlCommand cmd = con.CreateCommand())
                            {
                                cmd.CommandText = sql.ToString();
                                cmd.ExecuteNonQuery();
                            }
                        }
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        #endregion


    }
}
