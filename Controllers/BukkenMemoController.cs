using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Office.Interop.Excel;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;
using DataTable = System.Data.DataTable;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618
namespace web_groupware.Controllers
{
    [Authorize]
    public class BukkenMemoController : BaseController
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public BukkenMemoController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }
        /// <summary>
        /// 一覧画面表示
        /// </summary>
        /// <param name="model">BukkenMemoViewModel</param>
        /// <returns></returns
        public IActionResult Index(BukkenMemoViewModel model)
        {
            try
            {
                model = createViewModel(model.cond_bukken_name,model.cond_contract_status);
                return View(model);
            }
            catch (Exception ex) {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// 一覧画面用ViewModel作成
        /// </summary>
        /// <param name="bukken_name">検索条件（物件名）</param>
        /// <returns></returns
        public BukkenMemoViewModel createViewModel(string bukken_name,string cond_contract_status)
        {
            try
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine(" SELECT ");
                sql.AppendLine("  	B.bukn_cd ");
                sql.AppendLine("  	,B.bukn_name ");
                sql.AppendLine("  	,S2.staf_name ");
                sql.AppendLine("  	,S1.update_date  ");
                sql.AppendLine(" FROM M_BUKKEN B ");
                sql.AppendLine(" LEFT JOIN ");
                sql.AppendLine(" ( select bukn_cd,update_user,update_date,row_number() over(PARTITION BY bukn_cd ORDER BY update_date DESC) as num from T_BUKKENCOMMENT ) S1 ");
                sql.AppendLine(" ON B.bukn_cd = S1.bukn_cd ");
                sql.AppendLine(" LEFT JOIN M_STAFF S2 ");
                sql.AppendLine(" ON S1.update_user = S2.staf_cd ");

                sql.AppendLine(" LEFT JOIN ");
                sql.AppendLine(" ( select kanrikeiyaku_no,min(kaiyaku_kb) as kaiyaku_kb from T_KANRINFO group by kanrikeiyaku_no ) S3 ");
                sql.AppendLine(" ON B.bukn_cd = S3.kanrikeiyaku_no ");
                sql.AppendLine(" WHERE 1=1 ");
                sql.AppendLine(" AND (num =1 OR num IS NULL ) ");
                if (bukken_name != "")
                {
                    sql.AppendFormat(" AND (B.bukn_cd LIKE '%{0}%' OR B.bukn_name LIKE '%{0}%' )", bukken_name);
                }
                if (cond_contract_status!=null&& cond_contract_status == "0")
                {
                    sql.AppendFormat(" AND (S3.kaiyaku_kb IS NOT NULL AND S3.kaiyaku_kb = 0) ");
                }

                sql.AppendLine(" ORDER BY S1.update_date DESC, B.bukn_cd ");

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

                BukkenMemoViewModel model = new BukkenMemoViewModel();
                foreach (DataRow dr in dt.Rows)
                {
                    string message = "";
                    if (dr["update_date"] == DBNull.Value)
                    {
                        message = "コメント未作成";
                    }
                    else
                    {
                        message = GetWhenFromNow((DateTime)dr["update_date"]);
                    }
                    var records = _context.T_INFO_PERSONAL.Where(x =>x.parent_id== INFO_PERSONAL_PARENT_ID.T_BUKKENCOMMENT && x.first_no== dr.Field<decimal>("bukn_cd")&& x.staf_cd.ToString() == HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value && x.already_checked == false);
                    string count = records.Count() == 0 ? "" : records.Count().ToString();

                    model.list_bukken.Add(new BukkenMemo
                    {
                        bukn_cd = dr.Field<decimal>("bukn_cd"),
                        bukken_name = dr.Field<string>("bukn_name"),
                        update_user = dr.Field<string>("staf_name") ?? "コメント未作成",
                        update_date = message,
                        count = count
                    });

                }
                model.cond_bukken_name = bukken_name;
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// 詳細画面表示
        /// </summary>
        /// <param name="model">BukkenMemoDetailViewModel</param>
        /// <returns></returns
        [HttpGet]
        public IActionResult BukkenMemoDetail(BukkenMemoDetailViewModel model)
        {
            try
            {
                ModelState.Clear();
                model = createDetailViewModel(model.bukn_cd, model.cond_bukken_name);

                //workディレクトリ設定
                var dir_root = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.BUKKENCOMMENT_FILE)?.content;
                string dir_work = Path.Combine("work", model.bukn_cd.ToString(), HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                //workディレクトリの作成
                var dir_work_full = Path.Combine(dir_root, dir_work);
                Directory.CreateDirectory(dir_work_full);

                model.work_dir = dir_work;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        public List<T_BUKKENCOMMENT_FILE> GetRecoard_file(string comment_no)
        {
            List<T_BUKKENCOMMENT_FILE> record_file = _context.T_BUKKENCOMMENT_FILE.Where(x => x.comment_no.ToString() == comment_no).OrderBy(o => o.file_no).ToList();
            return record_file;
        }

        /// <summary>
        /// 詳細画面ViewModel作成
        /// </summary>
        /// <param name="bukn_cd">物件コード</param>
        /// <param name="cond_bukken_name">一覧画面検索条件（物件名）</param>
        /// <returns></returns
        public BukkenMemoDetailViewModel createDetailViewModel(int bukn_cd, string cond_bukken_name)
        {
            try
            {
                BukkenMemoDetailViewModel model = new BukkenMemoDetailViewModel();
                DataTable dt = new DataTable();
                StringBuilder sql = new StringBuilder();
                sql.AppendLine(" SELECT ");
                sql.AppendLine("  	B.bukn_cd ");
                sql.AppendLine("  	,B.bukn_name ");
                sql.AppendLine("  	,B.zip ");
                sql.AppendLine("  	,B.adr1 ");
                sql.AppendLine("  	,B.adr2 ");
                sql.AppendLine(" FROM M_BUKKEN B ");
                sql.AppendLine(" WHERE 1=1 ");
                sql.AppendFormat(" AND B.bukn_cd = {0} ", bukn_cd);

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
                model.bukn_cd = bukn_cd;
                model.bukken_name = dt.Rows[0].Field<string>("bukn_name");
                model.bukken_nameWithCode = dt.Rows[0].Field<string>("bukn_name") + "(" + dt.Rows[0].Field<decimal>("bukn_cd") + ")";
                model.zip = dt.Rows[0].Field<string>("zip") ?? "";
                model.address1 = dt.Rows[0].Field<string>("adr1") ?? "";
                model.address2 = dt.Rows[0].Field<string>("adr2") ?? "";

                model.dir_no = bukn_cd.ToString();
                createList(model);
                model.cond_bukken_name = cond_bukken_name;
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// 詳細画面ViewModelのコメントリスト作成
        /// </summary>
        /// <param name="model">BukkenMemoDetailViewModel</param>
        /// <returns></returns
        public BukkenMemoDetailViewModel createList(BukkenMemoDetailViewModel model)
        {
            try
            {
                DataTable dt = new DataTable();
                StringBuilder sql = new StringBuilder();
                sql.Clear();
                sql.AppendLine(" SELECT ");
                sql.AppendLine("  	S1.staf_name ");
                sql.AppendLine("  	,B.comment_no  ");
                sql.AppendLine("  	,B.update_date  ");
                sql.AppendLine("  	,B.message  ");
                sql.AppendLine(" FROM T_BUKKENCOMMENT B ");
                sql.AppendLine(" LEFT JOIN M_STAFF S1 ");
                sql.AppendLine(" ON B.update_user = S1.staf_cd ");
                sql.AppendLine(" WHERE 1=1 ");
                sql.AppendFormat(" AND B.bukn_cd = {0} ", model.bukn_cd);
                sql.AppendLine(" ORDER BY B.update_date DESC ");

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

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var T_INFO_PERSONAL = _context.T_INFO_PERSONAL.FirstOrDefault(x =>x.parent_id==INFO_PERSONAL_PARENT_ID.T_BUKKENCOMMENT&&x.first_no== model.bukn_cd&& x.second_no == dt.Rows[i].Field<int>("comment_no") && x.staf_cd.ToString() == HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                    var list_file = GetRecoard_file(dt.Rows[i].Field<int>("comment_no").ToString());
                    model.list_detail.Add(new BukkenMemoDetail()
                    {
                        comment_no = dt.Rows[i].Field<int>("comment_no"),
                        update_user = dt.Rows[i].Field<string>("staf_name"),
                        update_date = dt.Rows[i].Field<DateTime>("update_date").ToString("yyyy/M/d HH:mm"),
                        message = dt.Rows[i].Field<string>("message"),
                        already_read_comment = T_INFO_PERSONAL == null ? true : T_INFO_PERSONAL.already_checked,
                        List_T_BUKKENCOMMENT_FILE_ADDED =list_file

                }
                        );
                }
                return model;
            }
            catch (Exception ex) {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// コメント登録
        /// </summary>
        /// <param name="model">BukkenMemoDetailViewModel</param>
        /// <returns></returns
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BukkenMemoDetailViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    return View("BukkenMemoDetail", model);
                }
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var comment_no = GetNextNo(Utilities.DataTypes.BUKKEN_COMMENT_NO); var now = DateTime.Now;
                        var user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                        var record_new = new T_BUKKENCOMMENT();
                        record_new.bukn_cd = model.bukn_cd;
                        record_new.comment_no = comment_no;
                        record_new.message = model.message_new;
                        record_new.create_user = user;
                        record_new.create_date = now;
                        record_new.update_user = user;
                        record_new.update_date = now;
                        _context.T_BUKKENCOMMENT.Add(record_new);
                        _context.SaveChanges();

                        var t_staffm = await _context.M_STAFF.Where(x => x.retired != 1 && x.staf_cd.ToString() != HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value).ToListAsync();
                        for (int i = 0; i < t_staffm.Count; i++)
                        {
                            var record_comment_new = new T_INFO_PERSONAL();
                            record_comment_new.parent_id = INFO_PERSONAL_PARENT_ID.T_BUKKENCOMMENT;
                            record_comment_new.first_no = model.bukn_cd;
                            record_comment_new.second_no = comment_no;
                            record_comment_new.staf_cd = t_staffm[i].staf_cd;
                            record_comment_new.title = "物件コメント";
                            record_comment_new.content = model.message_new;
                            record_comment_new.url = string.Format("{0}://{1}{2}{3}/BukkenMemoDetail?bukn_cd={4}", HttpContext.Request.Scheme, HttpContext.Request.Host, HttpContext.Request.PathBase + "/", ControllerContext.ActionDescriptor.ControllerName, model.bukn_cd);
                            record_comment_new.create_user= user;
                            record_comment_new.create_date = now;
                            record_comment_new.update_user = user;
                            record_comment_new.update_date = now;
                            _context.T_INFO_PERSONAL.Add(record_comment_new);
                        }
                        _context.SaveChanges();

                        //ディレクトリ設定
                        var t_dic = await _context.M_DIC.FirstOrDefaultAsync(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.BUKKENCOMMENT_FILE);
                        var dir_root = t_dic.content;
                        string dir_main = Path.Combine(dir_root, model.bukn_cd.ToString(),comment_no.ToString());
                        if (!Directory.Exists(dir_main))
                        {
                            Directory.CreateDirectory(dir_main);
                        }

                        //対象T_BUKKENCOMMENT_FILEのレコード全削除
                        Dictionary<string, DateTime> dic_name_and_date_create = new Dictionary<string, DateTime>();
                        Dictionary<string, string> dic_name_and_user_create = new Dictionary<string, string>();
                        Dictionary<string, DateTime> dic_name_and_date = new Dictionary<string, DateTime>();
                        Dictionary<string, string> dic_name_and_user = new Dictionary<string, string>();
                        List<T_BUKKENCOMMENT_FILE> list_record_file = await _context.T_BUKKENCOMMENT_FILE.Where(x => x.comment_no == comment_no).ToListAsync();
                        foreach (T_BUKKENCOMMENT_FILE record_file in list_record_file)
                        {
                            dic_name_and_date_create.Add(record_file.fileName, record_file.create_date);
                            dic_name_and_user_create.Add(record_file.fileName, record_file.create_user);
                            dic_name_and_date.Add(record_file.fileName, record_file.update_date);
                            dic_name_and_user.Add(record_file.fileName, record_file.update_user);
                            _context.T_BUKKENCOMMENT_FILE.RemoveRange(record_file);
                        }
                        await _context.SaveChangesAsync();

                        //レコード登録前にmainからファイル削除
                        if (model.Delete_files != null)
                        {
                            var arr_delete_files = model.Delete_files.Split(':');
                            for (int i = 0; i < arr_delete_files.Length; i++)
                            {
                                if (arr_delete_files[i] != "")
                                {
                                    System.IO.File.Delete(Path.Combine(dir_main, arr_delete_files[i]));
                                }
                            }
                        }

                        //レコード登録　mainディレクトリ
                        foreach (string path_file in Directory.EnumerateFiles(dir_main))
                        {
                            var file_name = Path.GetFileName(path_file);
                            T_BUKKENCOMMENT_FILE record_file = null;
                            record_file = new T_BUKKENCOMMENT_FILE();
                            record_file.bukn_cd = model.bukn_cd;
                            record_file.comment_no = comment_no;
                            record_file.file_no = GetNextNo(DataTypes.BUKKENCOMMENT_FILE_NO); ;
                            record_file.fileName = file_name;
                            record_file.fullPath = path_file;
                            record_file.create_user = dic_name_and_user_create[file_name];
                            record_file.create_date = dic_name_and_date_create[file_name];
                            record_file.update_user = dic_name_and_user[file_name];
                            record_file.update_date = dic_name_and_date[file_name];
                            await _context.T_BUKKENCOMMENT_FILE.AddAsync(record_file);
                            await _context.SaveChangesAsync();
                        }
                        //レコード登録　workディレクトリ
                        var work_dir_files = Directory.GetFiles(Make_work_dir_full(model.dic_cd, model.work_dir));
                        for (int i = 0; i < work_dir_files.Count(); i++)
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
                                    kandidat = kandidat.Substring(0, kandidat.Length - 1);
                                    kandidat = kandidat + '（' + count + '）';
                                    // ファイルの拡張子を取得
                                    string fileExtention = Path.GetExtension(work_dir_files[i]);
                                    kandidat = kandidat + fileExtention;
                                    if (!System.IO.File.Exists(kandidat))
                                    {
                                        renamed_file = Path.Combine(Make_work_dir_full(model.dic_cd, model.work_dir), kandidat);
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

                            T_BUKKENCOMMENT_FILE record_file = null;
                            record_file = new T_BUKKENCOMMENT_FILE();
                            record_file.bukn_cd = model.bukn_cd;
                            record_file.comment_no = comment_no;
                            record_file.file_no = GetNextNo(DataTypes.BUKKENCOMMENT_FILE_NO);
                            record_file.fileName = file_name;
                            record_file.fullPath = Path.Combine(dir_main, file_name);
                            record_file.create_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            record_file.create_date = now;
                            record_file.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            record_file.update_date = now;
                            await _context.T_BUKKENCOMMENT_FILE.AddAsync(record_file);
                            await _context.SaveChangesAsync();

                            //ファイルをworkからmainにコピー
                            System.IO.File.Copy(work_dir_files[i], Path.Combine(dir_main, file_name));
                        }

                        Directory.Delete(Make_work_dir_full(model.dic_cd, model.work_dir), true);


                        tran.Commit();

                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        _logger.LogError(ex.Message);
                        _logger.LogError(ex.StackTrace);
                        tran.Dispose();
                        ModelState.AddModelError("", Message_register.FAILURE_001);
                        return View("BukkenMemoDetail", model);
                    }
                }
                var model_redirect = new BukkenMemoViewModel();
                model_redirect.cond_bukken_name = model.cond_bukken_name;
                return RedirectToAction("Index", "BukkenMemo", model_redirect);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// already_checked更新
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns
        public IActionResult Read_comment(BukkenMemoDetailViewModel model)
        {
            try
            {
                ModelState.Clear();
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        StringBuilder sql = new StringBuilder();
                        sql.AppendLine(" UPDATE ");
                        sql.AppendLine(" T_INFO_PERSONAL ");
                        sql.AppendLine(" SET already_checked=1");
                        sql.AppendFormat(" ,update_user = '{0}' ", HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
                        sql.AppendFormat(" ,update_date = '{0}' ", DateTime.Now);
                        sql.AppendLine(" WHERE 1=1 ");
                        sql.AppendFormat(" AND parent_id = {0} ", INFO_PERSONAL_PARENT_ID.T_BUKKENCOMMENT);
                        sql.AppendFormat(" AND first_no = {0} ", model.bukn_cd);
                        sql.AppendFormat(" AND second_no = {0} ", model.already_read_comment_no);
                        sql.AppendFormat(" AND staf_cd = {0} ", HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value);
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
                        ModelState.AddModelError("", Message_register.FAILURE_001);
                        //SetSelectListItem(model);
                        return View("BukkenMemoDetail", model);
                    }
                }
                return RedirectToAction("BukkenMemoDetail", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                ModelState.AddModelError("", Message_register.FAILURE_001);
                //SetSelectListItem(model);
                return View("BukkenMemoDetail", model);
            }
        }
    }
}
