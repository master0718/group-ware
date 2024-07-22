using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using web_groupware.Data;
using web_groupware.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using web_groupware.Utilities;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;

#pragma warning disable CS8600, CS8601, CS8602, CS8604, CS8618, CS8629
namespace web_groupware.Controllers
{
    public class BaseController : Controller
    {
        #region "member"
        /// <summary>
        /// データベース接続　サービス
        /// </summary>
        protected readonly web_groupwareContext _context;

        /// <summary>
        /// クライアントHTTP　サービス
        /// </summary>
        protected IHttpContextAccessor _httpContextAccessor;

        /// <summary>設定値</summary>
        protected IConfiguration _config;
        /// <summary>ログ出力</summary>
        protected readonly ILogger<BaseController> _logger;

        /// <summary>
        /// 実行環境
        /// </summary>
        IWebHostEnvironment _hostingEnvironment;

        #endregion
        private string _uploadPath = "";

        /// <summary>
        /// 本来のコンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="loginInfoRepository"></param>
        /// <param name="httpContextAccessor"></param>
        public BaseController(
            IConfiguration configuration,
            ILogger<BaseController> logger,
            web_groupwareContext context,
            IWebHostEnvironment hostingEnvironment,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _config = configuration;
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _hostingEnvironment = hostingEnvironment;

            var t_dic = _context.M_DIC
                .FirstOrDefault(m => m.dic_kb == DIC_KB.SAVE_PATH_FILE && m.dic_cd == DIC_KB_700_DIRECTORY.NOTICE);
            if (t_dic == null || t_dic.content == null)
            {
                _logger.LogError(Messages.ERROR_PREFIX + Messages.DICTIONARY_FILE_PATH_NO_EXIST, DIC_KB.SAVE_PATH_FILE, DIC_KB_700_DIRECTORY.NOTICE);
                throw new Exception(Messages.DICTIONARY_FILE_PATH_NO_EXIST);
            }
            else
            {
                _uploadPath = t_dic.content;
            }
        }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// T_NOから最新の番号を取得し、+1で更新
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public int GetNextNo(int data_type)
        {
            T_NO recoard_no = _context.T_NO.FirstOrDefault(x => x.data_type == data_type);
            if (recoard_no == null)
            {
                var item = new T_NO
                {
                    data_type = data_type,
                    no = 2,
                    comment = string.Empty
                };

                _context.T_NO.Add(item);
                _context.SaveChanges();

                return 1;
            }
            var nextNo = recoard_no.no;
            recoard_no.no = recoard_no.no + 1;
            _context.SaveChanges();
            return nextNo;

        }
        [Authorize]
        public async Task<IActionResult> GetGroupItems()
        {
            var groups = await _context.M_GROUP.ToListAsync();
            var result = new List<object>();

            foreach (var group in groups)
            {
                // Count the number of users in the group
                var userCount = await _context.T_GROUPSTAFF
                    .Where(gs => gs.group_cd == group.group_cd)
                    .Select(gs => gs.staf_cd) // Select the user IDs
                    .Distinct() // Ensure unique user IDs
                    .CountAsync(); // Count the distinct users

                result.Add(new
                {
                    group.group_cd,
                    group.group_name,
                    user_count = userCount
                });
            }

            return Json(result);
        }

        /// <summary>
        /// 社内通知取得
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<string> GetReportCount()
        {
            //frist_no==1 日報
            //first_no==2 日報コメント
            //second_no== 日報番号
            var staf_cd = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
            var t_report = await Task.Run(() => _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORT && x.staf_cd.ToString() == staf_cd && x.already_read == false).Count());
            var t_reportcomment = await Task.Run(() => _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_REPORTCOMMENT && x.staf_cd.ToString() == staf_cd && x.already_read == false).Count());
            string count = t_report + t_reportcomment == 0 ? "" : (t_report + t_reportcomment).ToString();
            return count;

        }
        /// <summary>
        /// 物件メモ未読
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<string> GetBukkenCommentReadCount()
        {
            //var staf_cd= int.Parse(HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value);
            var staf_cd = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
            var records = await Task.Run(() => _context.T_INFO_PERSONAL.Where(x => x.parent_id == INFO_PERSONAL_PARENT_ID.T_BUKKENCOMMENT && x.staf_cd == int.Parse(staf_cd) && x.already_read == false && x.update_user != staf_cd.ToString()));
            //_context.T_REPORTCOMMENT_READ.Where(x => x.staf_cd == staf_cd && x.alreadyread_flg ==false && x.update_user!=staf_cd.ToString());
            string count = records.Count() == 0 ? "" : records.Count().ToString();
            return count;
        }
        /// <summary>
        /// 社員検索ダイアログ
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> DialogStaff(BaseDialogStaffViewModel model)
        {
            try
            {
                var t_group = await _context.M_GROUP.ToListAsync();
                for (int g = 0; g < t_group.Count; g++)
                {
                    var group = new SelectListItem()
                    {
                        Value = t_group[g].group_cd.ToString(),
                        Text = t_group[g].group_name
                    };
                    model.List_group_cd.Add(group);
                }
                var t_staff = _context.M_STAFF.Where(x => x.staf_cd.ToString() != HttpContext.User.FindFirst(ClaimTypes.STAF_CD).Value).Select(x => new { x.staf_cd, x.staf_name });
                if (model.Selected_group_cd != null && model.Selected_group_cd != "-1")
                {
                    t_staff = t_staff.Join(_context.T_GROUPSTAFF, x => x.staf_cd, y => y.staf_cd, (x, y) => new
                    {
                        x.staf_cd,
                        x.staf_name,
                        y.group_cd
                    }).Where(x => x.group_cd.ToString() == model.Selected_group_cd).Select(x => new { x.staf_cd, x.staf_name });
                }
                var list = t_staff.ToList();
                for (int i = 0; i < t_staff.Count(); i++)
                {
                    var staff = new BaseDialogStaffCheckBoxModel()
                    {
                        Staf_cd = list[i].staf_cd,
                        Staf_name = list[i].staf_name,
                        Is_checked = false
                    };
                    model.List_staf_cd.Add(staff);
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;

            }
        }



        /// <summary>
        /// 差分メッセージ取得
        /// </summary>
        /// <param name="ymd"></param>
        /// <returns></returns
        public string GetWhenFromNow(DateTime ymd)
        {
            string message;
            DateTime now = DateTime.Now;

            DateTime lastYear1st = new DateTime(now.AddYears(-1).Year, 1, 1);
            DateTime thisYear1st = new DateTime(now.Year, 1, 1);
            DateTime thisMonth1st = new DateTime(now.Year, now.Month, 1);
            DateTime today0am = new DateTime(now.Year, now.Month, now.Day);
            var second = GetDATEDIFF("second", ymd, now);
            if (ymd < lastYear1st)
            {
                message = "去年以前";
            }
            else if (ymd < thisYear1st)
            {
                message = "去年";
            }
            else if (GetDATEDIFF("month", ymd, now) > 1)
            {
                message = GetDATEDIFF("month", ymd, now) + "か月前";
            }
            else if (ymd < thisMonth1st)
            {
                message = "先月";
            }
            else if (second > 604800)
            {
                message = second / 604800 + "週間前";
            }
            else if (second >= 86400)
            {
                message = second / 86400 + "日前";
            }
            else if (ymd < today0am)
            {
                message = "昨日";
            }
            else if (second >= 3600)
            {
                message = second / 3600 + "時間前";
            }
            else
            {
                message = second / 60 + "分前";
            }
            return message;

        }

        /// <summary>
        /// 月差分取得（C#にDATEDIFFが存在しないのでSQLで代用）
        /// </summary>
        /// <param name="span">day,month,year</param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns
        public int GetDATEDIFF(string span, DateTime start, DateTime end)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine(" SELECT ");
            sql.AppendFormat(" DATEDIFF({0}, '{1}', '{2}') as diff ", span, start, end);
            var dt = new DataTable();
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
            return dt.Rows[0].Field<int>("diff");

        }
        /// <summary>
        /// ファイルアップロード
        /// </summary>
        /// <param name="work_dir">ワークディレクトリ</param>
        /// <param name="file">ファイル</param>
        [HttpPost]
        public IActionResult UploadFile(string dic_cd, string work_dir, IFormFile file)
        {
            try
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd);
                        var dir_root = t_dic.content;
                        work_dir = Path.Combine(dir_root, work_dir);

                        string path_file = Path.Combine(work_dir, file.FileName);

                        //ファイルコピー
                        using (var fileStream = new FileStream(path_file, FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        tran.Commit();
                        return Ok();

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
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }
        /// <summary>
        /// ファイルプレビュー
        /// </summary>
        /// <param name="dic_cd">dic_cd</param>
        /// <param name="dir_no">ルートディレクトリの下のディレクトリ名</param>
        /// <param name="file_name">ファイル名</param>
        [HttpGet]
        public IActionResult PreviewFile(string dic_cd, string dir_no, string file_name)
        {
            try
            {
                var root_dir = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd).content;
                var fullPath = Path.Combine(root_dir, dir_no, file_name);

                var model = new BasePreviewFile();
                model.dic_cd = dic_cd;
                model.dir_no = dir_no;
                model.file_name = file_name;
                return View("DialogPreviewFile", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// ファイルダウンロード
        /// </summary>
        /// <param name="dic_cd">dic_cd</param>
        /// <param name="dir_no">ルートディレクトリの下のディレクトリ名</param>
        /// <param name="file_name">ファイル名</param>
        [HttpGet]
        public IActionResult DownloadFile_stream(string dic_cd, string dir_no, string file_name)
        {
            try
            {
                var root_dir = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd).content;
                var fullPath = Path.Combine(root_dir, dir_no, file_name);
                new FileExtensionContentTypeProvider()
                                .TryGetContentType(fullPath, out string contentType);
                if (contentType == null) contentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                FileStream stream = new FileStream(fullPath, FileMode.Open);
                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// ファイルダウンロード
        /// </summary>
        /// <param name="dic_cd">dic_cd</param>
        /// <param name="dir_no">ルートディレクトリの下のディレクトリ名</param>
        /// <param name="file_name">ファイル名</param>
        [HttpGet]
        public IActionResult DownloadFile(string dic_cd, string dir_no, string file_name)
        {
            try
            {
                //var fullPath = _context.T_INFO_FILE.FirstOrDefault(x => x.info_cd == info_cd && x.fileName == file_name).fullPath;
                var root_dir = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd).content;
                var fullPath = Path.Combine(root_dir, dir_no, file_name);
                //var fileName = Path.GetFileName(fullPath);
                var content = System.IO.File.ReadAllBytes(fullPath);
                new FileExtensionContentTypeProvider()
                                .TryGetContentType(fullPath, out string contentType);
                if (contentType == null) contentType = System.Net.Mime.MediaTypeNames.Application.Octet;

                return File(content, contentType, file_name);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult DownloadFileM(int file_no)
        {
            try
            {
                var root_dir = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.FILEINFO).content;

                var fileInfo = _context.T_FILEINFO.FirstOrDefault(x => x.file_no == file_no);
                var path = fileInfo.path == "共有フォルダ" ? "" : fileInfo.path;
                var fileName = fileInfo.name;
                var fullPath = Path.Combine(root_dir, path, fileName);

                var content = System.IO.File.ReadAllBytes(fullPath);
                new FileExtensionContentTypeProvider()
                                .TryGetContentType(fullPath, out string contentType);
                if (contentType == null) contentType = System.Net.Mime.MediaTypeNames.Application.Octet;

                return File(content, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// ファイル削除
        /// </summary>
        /// <param name="work_dir"></param>
        /// <param name="file_name"></param>
        [HttpPost]
        public IActionResult DeleteFile(string dic_cd, string work_dir, string file_name)
        {
            try
            {
                var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd);
                var dir_root = t_dic.content;
                work_dir = Path.Combine(dir_root, work_dir);

                System.IO.File.Delete(Path.Combine(work_dir, file_name));

                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// フォルダ削除
        /// </summary>
        /// <param name="work_dir">削除対象フォルダパス</param>
        //[HttpGet]
        public IActionResult DeleteDirectory(string dic_cd, string work_dir)
        {
            try
            {
                var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd);
                var dir_root = t_dic.content;
                work_dir = Path.Combine(dir_root, work_dir);
                if (Directory.Exists(work_dir))
                    Directory.Delete(work_dir, true);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// フォルダの全てのファイルの削除
        /// </summary>
        /// <param name="work_dir">削除対象フォルダパス</param>
        [HttpGet]
        public IActionResult ClearDirectory(string dic, string work_dir)
        {
            try
            {
                var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic);
                var dir_root = t_dic.content;
                work_dir = Path.Combine(dir_root, work_dir);

                string[] files = Directory.GetFiles(work_dir);

                // Delete each file
                foreach (string file in files)
                {
                    System.IO.File.Delete(file);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        protected void pdfFileToImg(string pdf_path)
        {
#pragma warning disable CA1416
            var user_id = User.FindFirst(ClaimTypes.STAF_CD).Value;
            //var t_staff = _context.M_STAFF.FirstOrDefault(x => x.staf_cd == Convert.ToInt32(HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value));
            var dir_work_img = Path.Combine(_uploadPath, Path.Combine("work", user_id, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")));
            if (!Directory.Exists(dir_work_img))
            {
                Directory.CreateDirectory(dir_work_img);
            }

            int IMG_DPI = 150;
            //PDFを開く
            PdfDocument pdf_doc = PdfDocument.Load(pdf_path);

            for (int i = 0; i < pdf_doc.PageCount; i++)
            {
                //画像変換
                Image img = pdf_doc.Render(i, IMG_DPI, IMG_DPI, PdfRenderFlags.CorrectFromDpi);

                //保存先ファイル名（元ファイル名の拡張子を ".jpeg" へ）
                string fn_img_full = Path.ChangeExtension(Path.Combine(dir_work_img, Path.GetFileNameWithoutExtension(pdf_path) + i.ToString() + Path.GetExtension(pdf_path)), ".jpeg");

                //保存
                img.Save(fn_img_full, ImageFormat.Jpeg);
            }
            pdf_doc.Dispose();

            //ワーク
            var arr_jmages = System.IO.Directory.GetFiles(dir_work_img);
            var list_Images = new List<Bitmap>();
            for (int i = 0; i < arr_jmages.Count(); i++)
            {
                list_Images.Add(new Bitmap(arr_jmages[i]));
            }

            // 画像の結合
            var combinedImage = ImageCombineV(list_Images);

            // 結合画像の保存
            combinedImage.Save(Path.ChangeExtension(pdf_path, ".jpeg"), System.Drawing.Imaging.ImageFormat.Jpeg);

            // 解放
            foreach (var bmp in list_Images)
            {
                bmp.Dispose();
            }
            Directory.Delete(dir_work_img, true);
        }

        /// <summary>
        /// 画像を縦に結合する
        /// </summary>
        /// <param name="src">結合するBitmapのList</param>
        /// <returns>結合されたBitmapオブジェクト</returns>
        [Authorize]
        protected Bitmap ImageCombineV(List<Bitmap> src)
        {
            // 結合後のサイズを計算
            int dstWidth = 0, dstHeight = 0;
            System.Drawing.Imaging.PixelFormat dstPixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;

            for (int i = 0; i < src.Count(); i++)
            {
                if (dstWidth < src[i].Width) dstWidth = src[i].Width;
                dstHeight += src[i].Height;

                // 最大のビット数を検索
                if (Image.GetPixelFormatSize(dstPixelFormat)
                    < Image.GetPixelFormatSize(src[i].PixelFormat))
                {
                    dstPixelFormat = src[i].PixelFormat;
                }
            }

            var dst = new Bitmap(dstWidth, dstHeight, dstPixelFormat);
            var dstRect = new Rectangle();

            using (var g = Graphics.FromImage(dst))
            {
                for (int i = 0; i < src.Count(); i++)
                {
                    dstRect.Width = src[i].Width;
                    dstRect.Height = src[i].Height;

                    // 描画
                    g.DrawImage(src[i], dstRect, 0, 0, src[i].Width, src[i].Height, GraphicsUnit.Pixel);

                    // 次の描画先
                    dstRect.Y = dstRect.Bottom;
                }
            }
            return dst;
        }

        protected void ResetWorkDir(string dic_cd, string work_dir)
        {
            try
            {
                var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd);
                var dir_root = t_dic.content;
                var work_dir_full = Path.Combine(dir_root, work_dir);

                if (Directory.Exists(work_dir))
                    Directory.Delete(work_dir_full, true);
                Directory.CreateDirectory(work_dir_full);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// dic_cdとwork_dicからワークディレクトリのフルパス作成
        /// </summary>
        /// <param name="dic_cd">dic_cd</param>
        /// <param name="work_dir">ワークディレクトリ</param>
        public string Make_work_dir_full(string dic_cd, string work_dir)
        {
            try
            {
                var t_dic = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == dic_cd);
                var dir_root = t_dic.content;
                var work_dir_full = Path.Combine(dir_root, work_dir);

                return work_dir_full;
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                // OnActionExecuting に相当する処理
                Debug.WriteLine("OnActionExecuting");

                await next();

                if (User.Identity.AuthenticationType == "AstIdentity")
                {
                    var t_staff = _context.M_STAFF.FirstOrDefault(x => x.staf_cd.ToString() == User.FindFirst(ClaimTypes.STAF_CD).Value);
                    if (t_staff != null)
                    {
                        TempData["Base_staff_name"] = t_staff.staf_name;
                    }
                    var t_staff_auth = _context.M_STAFF_AUTH.FirstOrDefault(x => x.staf_cd.ToString() == User.FindFirst(ClaimTypes.STAF_CD).Value);
                    if (t_staff_auth != null)
                    {
                        TempData["Base_auth_admin"] = t_staff_auth.auth_admin;
                        TempData["Base_workflow_auth"] = t_staff_auth.workflow_auth;
                    }
                    else
                    {
                        TempData["Base_auth_admin"] = "0";
                        TempData["Base_workflow_auth"] = "0";
                    }

                    var systems = _context.M_SYSTEM.ToList();
                    var model = new List<List<string>>();
                    foreach (var system in systems)
                    {
                        var item = _context.M_STAFF_SYSTEMAUTH.Where(x => x.staf_cd == t_staff.staf_cd && x.system_id == system.system_id).FirstOrDefault();
                        if (item != null && item.onoff)
                        {
                            List<string> list_url_name = new List<string>();
                            list_url_name.Add(system.system_url);
                            list_url_name.Add(system.system_name);

                            model.Add(list_url_name);
                        }
                    }
                    TempData["Base_project"] = JsonSerializer.Serialize(model);
                    //社内連絡
                    var notice = await _context.T_INFO.FirstOrDefaultAsync(x => x.info_cd == 2);
                    TempData["Base_top_info_message"] = notice == null ? "" : notice.message;
                    var list_file = new List<List<string>>();
                    var files = _context.T_INFO_FILE.Where(x => x.info_cd == 2).ToList();
                    for (int i = 0; i < files.Count; i++)
                    {
                        List<string> list_dic_cd_dir_no_fileName = new List<string>();
                        list_dic_cd_dir_no_fileName.Add(DIC_KB_700_DIRECTORY.NOTICE);
                        list_dic_cd_dir_no_fileName.Add("2");
                        list_dic_cd_dir_no_fileName.Add(files[i].fileName);
                        list_file.Add(list_dic_cd_dir_no_fileName);
                    }
                    TempData["Base_top_info_file"] = JsonSerializer.Serialize(list_file);
                    //日報未読件数
                    var count_report = GetReportCount().Result;
                    TempData["Base_count_report"] = count_report;
                    //物件メモ未読件数
                    var count_bukken_memo = GetBukkenCommentReadCount().Result;
                    TempData["Base_count_bukken_memo"] = count_bukken_memo;
                }

                // OnActionExecuted に相当する処理
                Debug.WriteLine("OnActionExecuted");
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [Authorize]
        public IActionResult GetHolidays(int request_no)
        {
            var holidays = _context.T_HOLIDAY
                .OrderBy(x => x.holiday_date)
                .Select(x => x.holiday_date.ToString("yyyy-MM-dd"))
                .ToList();

            return Ok(holidays);
        }
    }
}