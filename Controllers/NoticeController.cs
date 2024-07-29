using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Controllers
{
    [Authorize]
    public class NoticeController : BaseController
    {
        const int info_cd = 2;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public NoticeController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }

        /// <summary>
        /// 初期表示
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                //workディレクトリ設定
                var dir_root = _context.M_DIC.FirstOrDefault(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.NOTICE)?.content;
                string dir_work = Path.Combine("work", info_cd.ToString(), HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                //workディレクトリの作成
                var dir_work_full = Path.Combine(dir_root, dir_work);
                Directory.CreateDirectory(dir_work_full);
                //画面に渡すモデル作成
                var recoard = _context.T_INFO.FirstOrDefault(x => x.info_cd == info_cd);
                var recoard_file = GetRecoard_file();
                var model = new NoticeViewModel();
                model.Message = recoard == null ? null : recoard.message;
                model.dir_no=info_cd.ToString();
                model.work_dir = dir_work;
                model.List_T_INFO_FILE = recoard_file;
                model.Upload_file_allowed_extension_1 = UPLOAD_FILE_ALLOWED_EXTENSION.IMAGE_PDF;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// 登録・変更
        /// </summary>
        /// <param name="file_name">NoticeViewModel</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoticeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                    ResetWorkDir(model.dic_cd, model.work_dir);
                    model.List_T_INFO_FILE = GetRecoard_file();
                    return View("Index", model);
                }

                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;
                        //T_INFO　登録・変更
                        var recoard = await _context.T_INFO.FirstOrDefaultAsync(x => x.info_cd == info_cd);
                        if (recoard == null)
                        {
                            var recoard_new = new T_INFO();
                            recoard_new.info_cd = info_cd;
                            recoard_new.message = model.Message;
                            recoard_new.create_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            recoard_new.create_date = now;
                            recoard_new.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            recoard_new.update_date = now;
                            await _context.T_INFO.AddAsync(recoard_new);
                            await _context.SaveChangesAsync();

                        }
                        else
                        {
                            recoard.message = model.Message;
                            recoard.create_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            recoard.create_date = now;
                            recoard.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            recoard.update_date = now;
                            await _context.SaveChangesAsync();
                        }

                        //ファイルに関する処理
                        List<string> path_uploadFile = new List<string>();
                        //ディレクトリ設定
                        var t_dic = await _context.M_DIC.FirstOrDefaultAsync(x => x.dic_kb == DIC_KB.SAVE_PATH_FILE && x.dic_cd == DIC_KB_700_DIRECTORY.NOTICE);
                        var dir_root = t_dic.content;
                        string dir_main = Path.Combine(dir_root, info_cd.ToString());
                        if (!Directory.Exists(dir_main))
                        {
                            Directory.CreateDirectory(dir_main);
                        }

                        //対象T_INFO_FILEのレコード全削除
                        Dictionary<string, DateTime?> dic_name_and_date_create = new Dictionary<string, DateTime?>();
                        Dictionary<string, string> dic_name_and_user_create = new Dictionary<string, string>();
                        Dictionary<string, DateTime?> dic_name_and_date = new Dictionary<string, DateTime?>();
                        Dictionary<string, string> dic_name_and_user = new Dictionary<string, string>();
                        List<T_INFO_FILE> list_recoard_file = await _context.T_INFO_FILE.Where(x => x.info_cd == info_cd).ToListAsync();
                        foreach (T_INFO_FILE record_file in list_recoard_file)
                        {
                            dic_name_and_date_create.Add(record_file.fileName, record_file.create_date);
                            dic_name_and_user_create.Add(record_file.fileName, record_file.create_user);
                            dic_name_and_date.Add(record_file.fileName, record_file.update_date);
                            dic_name_and_user.Add(record_file.fileName, record_file.update_user);
                            _context.T_INFO_FILE.RemoveRange(record_file);
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
                            T_INFO_FILE recoard_file = null;
                            recoard_file = new T_INFO_FILE();
                            recoard_file.file_no = GetNextNo(Utilities.DataTypes.INFO_FILE_NO);
                            recoard_file.info_cd = info_cd;
                            recoard_file.fileName = file_name;
                            recoard_file.fullPath = path_file;
                            recoard_file.create_user = dic_name_and_user_create[file_name];
                            recoard_file.create_date = dic_name_and_date_create[file_name];
                            recoard_file.update_user = dic_name_and_user[file_name];
                            recoard_file.update_date = dic_name_and_date[file_name];
                            await _context.T_INFO_FILE.AddAsync(recoard_file);
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
                            T_INFO_FILE recoard_file = null;
                            recoard_file = new T_INFO_FILE();
                            recoard_file.file_no = GetNextNo(Utilities.DataTypes.INFO_FILE_NO);
                            recoard_file.info_cd = info_cd;
                            recoard_file.fileName = file_name;
                            recoard_file.fullPath = Path.Combine(dir_main, file_name);
                            recoard_file.create_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            recoard_file.create_date = now;
                            recoard_file.update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value;
                            recoard_file.update_date = now;
                            await _context.T_INFO_FILE.AddAsync(recoard_file);
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
                        Directory.Delete(Make_work_dir_full(model.dic_cd, model.work_dir), true);
                        Directory.CreateDirectory(Make_work_dir_full(model.dic_cd, model.work_dir));
                        ModelState.AddModelError("", Message_register.FAILURE_001);
                        model.List_T_INFO_FILE = GetRecoard_file();
                        return View("Index", model);
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                Directory.Delete(Make_work_dir_full(model.dic_cd, model.work_dir), true);
                Directory.CreateDirectory(Make_work_dir_full(model.dic_cd, model.work_dir));
                ModelState.AddModelError("", Message_register.FAILURE_001);
                model.List_T_INFO_FILE = GetRecoard_file();
                return View("Index", model);
            }

        }

        public List<T_INFO_FILE> GetRecoard_file()
        {
            List<T_INFO_FILE> recoard_file = _context.T_INFO_FILE.Where(x => x.info_cd == info_cd).OrderBy(o => o.file_no).ToList();
            return recoard_file;
        }

    }
}
