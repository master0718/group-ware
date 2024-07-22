using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618
namespace web_groupware.Controllers
{
    [Authorize]
    public class RestrationReportController : BaseController
    {
        const int SBT_NAISO = 19;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public RestrationReportController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                RestrationReportViewModel model = createViewModel(DateTime.Now.Date.AddMonths(-1), null, null, null, null);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
        [HttpPost]
        public async Task<IActionResult> Index(RestrationReportViewModel model)
        {
            try
            {
                model = createViewModel(model.cond_leaving_date_from, model.cond_leaving_date_to, model.cond_bukken_cd, model.cond_bukken_name, model.cond_staf_cd);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        public RestrationReportViewModel createViewModel(DateTime? cond_leaving_date_from, DateTime? cond_leaving_date_to, string? cond_bukken_cd, string? cond_bukken_name, string? cond_staf_cd)
        {
            try
            {
                RestrationReportViewModel model = new RestrationReportViewModel();
                model.cond_leaving_date_from = cond_leaving_date_from;
                model.cond_leaving_date_to = cond_leaving_date_to;
                model.cond_bukken_cd = cond_bukken_cd;
                model.cond_bukken_name = cond_bukken_name;
                model.cond_staf_cd = cond_staf_cd;

                StringBuilder sql = new StringBuilder();
                sql.Append("SELECT ");

                sql.AppendLine(" 	[dbo].[FN_KEIYAKU_3034](HED.n_keiyaku_no) as taikyo_seisou, ");
                sql.AppendLine(" 	[dbo].[FN_KEIYAKU_3035](HED.n_keiyaku_no) ff_seibi, ");

                sql.Append("  HED.taikyo_ymd AS taikyo_ymd, ");
                sql.Append("  HED.bukn_cd AS bukn_cd, ");
                sql.Append("  BUK.bukn_ryaku AS bukn_name, ");
                sql.Append("  CONVERT(varchar, HED.room_no) AS room_no, ");
                sql.Append("  GYONS.gyos_name AS HS_gyos_name, ");
                sql.Append("  DTLNS.sitei_ymd AS HS_yotei_ymd, ");
                sql.Append("  CASE ");
                sql.Append("    WHEN DTLNS.cnt = FINNS.cnt THEN FINNS.kanryo_ymd ");
                sql.Append("    ELSE NULL ");
                sql.Append("  END AS HS_kanryo_ymd, ");
                sql.Append("  FORMAT(DTLNS.gseikyu_ymd, 'yy/M') AS HS_seikyu_month, ");
                sql.Append("  ISNULL(DTLNS.siharai_kin ,0) AS HS_siharai_kin, ");
                sql.Append("  HED.sagyo_naiyo AS sagyo_naiyo, ");
                sql.Append("  ISNULL(FTNNS.futan_kin, 0) AS HS_seikyu_kin, ");
                sql.Append("  KAIOWNS.yotei_ymd AS HS_kaiyotei_ow_ymd, ");
                sql.Append("  KAIKANS.yotei_ymd AS HS_kaiyotei_ka_ymd, ");
                sql.Append("  GYOBS.gyos_name AS BS_gyos_name, ");
                sql.Append("  DTLBS.sitei_ymd AS BS_yotei_ymd, ");
                sql.Append("  CASE ");
                sql.Append("    WHEN DTLBS.cnt = FINBS.cnt THEN FINBS.kanryo_ymd ");
                sql.Append("    ELSE NULL ");
                sql.Append("  END AS BS_kanryo_ymd, ");
                sql.Append("  GYOOH.gyos_name AS OH_gyos_name, ");
                sql.Append("  DTLOH.sitei_ymd AS OH_yotei_ymd, ");
                sql.Append("  CASE ");
                sql.Append("    WHEN DTLOH.cnt = FINOH.cnt THEN FINOH.kanryo_ymd ");
                sql.Append("    ELSE NULL ");
                sql.Append("  END AS OH_kanryo_ymd, ");
                sql.Append("  SYA.staf_name AS hachusya_nm, ");
                sql.Append("  RSTS.nyukyo_ymd AS nyukyo_ymd, ");
                sql.Append("  CASE HED.counter_paint ");
                sql.Append("   WHEN 1 THEN '有' ");
                sql.Append("   ELSE '' ");
                sql.Append("  END AS counter, ");
                sql.Append("  HED.counter_paint, ");
                sql.Append("  ISNULL(DTLNS.cnt, 0) AS DTLNS_cnt, ");
                sql.Append("  ISNULL(FINNS.cnt, 0) AS FINNS_cnt, ");
                sql.Append("  ISNULL(SEINS.cnt, 0) AS SEINS_cnt, ");
                sql.Append("  HED.hachu_no AS hachu_no ");

                // 発注基本情報
                sql.Append(" FROM T_HATTINFO HED ");
                // 物件基本情報
                sql.Append(" INNER JOIN M_BUKKEN BUK ");
                sql.Append("  ON  HED.bukn_cd = BUK.bukn_cd ");

                sql.AppendLine(" LEFT JOIN ( ");
                sql.AppendFormat(" 	select * from {0} where kamk_cd = 3034 ", "enkketi");
                sql.AppendLine(" 	) as  sub_1 on HED.n_keiyaku_no = sub_1.n_keiyaku_no ");
                sql.AppendLine(" LEFT JOIN ( ");
                sql.AppendFormat(" 	select * from {0} where kamk_cd = 3035 ", "enkketi");
                sql.AppendLine(" 	) as  sub_2 on HED.n_keiyaku_no = sub_2.n_keiyaku_no ");

                // 社員マスタ
                sql.Append(" LEFT JOIN M_STAFF SYA ");
                sql.Append("  ON  HED.hachusya_cd = SYA.staf_cd ");
                // ルーム空室情報
                sql.Append(" LEFT JOIN T_ROOMSTS RSTS ");
                sql.Append("  ON  HED.n_keiyaku_no = RSTS.n_keiyaku_no ");

                // 内装関連情報
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, MIN(hachu_eda) AS hachu_eda, MAX(sitei_ymd) AS sitei_ymd, SUM(koji_kin) AS koji_kin, SUM(siharai_kin) AS siharai_kin, MAX(gseikyu_ymd) AS gseikyu_ymd, COUNT(*) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");
                sql.AppendFormat(" AND sagyo_sb = {0} ", SBT_NAISO);
                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) DTLNS ");
                sql.Append("  ON  HED.hachu_no = DTLNS.hachu_no ");
                // 内装関連負担金
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT AA.hachu_no, SUM(BB.futan_kin) AS futan_kin ");
                sql.Append("     FROM T_HATTDTL AA");
                sql.Append("     INNER JOIN T_HATTFUTA BB ");
                sql.Append("       ON  AA.hachu_no = BB.hachu_no ");
                sql.Append("       AND AA.hachu_eda = BB.hachu_eda ");
                sql.Append("       AND BB.futan_kb IN (1, 2, 4)");
                sql.Append("     WHERE teishi_kb = 0 ");
                sql.AppendFormat(" AND sagyo_sb = {0} ", SBT_NAISO);
                sql.Append("     GROUP BY AA.hachu_no ");
                sql.Append(" ) FTNNS ");
                sql.Append("  ON  HED.hachu_no = FTNNS.hachu_no ");
                // 若番業者
                sql.Append(" LEFT JOIN T_HATTDTL EDANS ");
                sql.Append("  ON  DTLNS.hachu_no = EDANS.hachu_no ");
                sql.Append("  AND DTLNS.hachu_eda = EDANS.hachu_eda ");
                sql.Append(" LEFT JOIN T_GYOSYAM GYONS ");
                sql.Append("  ON  EDANS.gyos_cd = GYONS.gyos_cd ");
                // 完了済み作業
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, MAX(kanryo_ymd) AS kanryo_ymd, COUNT(*) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");
                sql.AppendFormat(" AND sagyo_sb = {0} ", SBT_NAISO);
                sql.Append("       AND kanryo_ymd IS NOT NULL ");
                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) FINNS ");
                sql.Append("  ON  HED.hachu_no = FINNS.hachu_no ");
                // 業者からの請求確定作業
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, COUNT(DISTINCT FORMAT(gseikyu_ymd, 'yy/M')) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");
                sql.AppendFormat(" AND sagyo_sb = {0} ", SBT_NAISO);
                sql.Append("       AND gseikyu_ymd IS NOT NULL ");
                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) SEINS ");
                sql.Append("  ON  HED.hachu_no = SEINS.hachu_no ");
                // オーナー／アスタ回収予定
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT DTLOW.hachu_no, MAX(KAIOW.yotei_ymd) AS yotei_ymd ");
                sql.Append("     FROM T_HATTDTL DTLOW ");
                sql.Append("     INNER JOIN T_HATTKAI KAIOW ");
                sql.Append("       ON  DTLOW.hachu_no = KAIOW.hachu_no ");
                sql.Append("       AND ((DTLOW.sagyo_kb = KAIOW.sagyo_kb) OR (KAIOW.sagyo_kb = 0)) ");
                sql.Append("       AND KAIOW.futan_kb IN(1,4) ");
                sql.Append("     WHERE DTLOW.teishi_kb = 0 ");
                sql.AppendFormat(" AND DTLOW.sagyo_sb = {0} ", SBT_NAISO);
                sql.Append("     GROUP BY DTLOW.hachu_no ");
                sql.Append(" ) KAIOWNS ");
                sql.Append("  ON  HED.hachu_no = KAIOWNS.hachu_no ");
                // 借主回収予定
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT DTLKA.hachu_no, MAX(yotei_ymd) AS yotei_ymd ");
                sql.Append("     FROM T_HATTDTL DTLKA ");
                sql.Append("     INNER JOIN T_HATTKAI KAIKA ");
                sql.Append("       ON  DTLKA.hachu_no = KAIKA.hachu_no ");
                sql.Append("       AND ((DTLKA.sagyo_kb = KAIKA.sagyo_kb) OR (KAIKA.sagyo_kb = 0)) ");
                sql.Append("       AND KAIKA.futan_kb = 2 ");
                sql.Append("     WHERE DTLKA.teishi_kb = 0 ");
                sql.AppendFormat(" AND DTLKA.sagyo_sb = {0} ", SBT_NAISO);
                sql.Append("     GROUP BY DTLKA.hachu_no ");
                sql.Append(" ) KAIKANS ");
                sql.Append("  ON  HED.hachu_no = KAIKANS.hachu_no ");

                // 美装関連情報
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, MIN(hachu_eda) AS hachu_eda, MAX(sitei_ymd) AS sitei_ymd, COUNT(*) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");

                sql.Append("       AND sagyo_sb in (1,24) ");

                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) DTLBS ");
                sql.Append("  ON  HED.hachu_no = DTLBS.hachu_no ");
                // 若番業者
                sql.Append(" LEFT JOIN T_HATTDTL EDABS ");
                sql.Append("  ON  DTLBS.hachu_no = EDABS.hachu_no ");
                sql.Append("  AND DTLBS.hachu_eda = EDABS.hachu_eda ");
                sql.Append(" LEFT JOIN T_GYOSYAM GYOBS ");
                sql.Append("  ON  EDABS.gyos_cd = GYOBS.gyos_cd ");
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, MAX(kanryo_ymd) AS kanryo_ymd, COUNT(*) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");

                sql.Append("       AND sagyo_sb in( 1 ,24 )");

                sql.Append("       AND kanryo_ymd IS NOT NULL ");
                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) FINBS ");
                sql.Append("  ON  HED.hachu_no = FINBS.hachu_no ");

                // ストーブ清掃関連情報
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, MIN(hachu_eda) AS hachu_eda, MAX(sitei_ymd) AS sitei_ymd, COUNT(*) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");
                sql.Append("       AND sagyo_sb = 2 ");
                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) DTLOH ");
                sql.Append("  ON  HED.hachu_no = DTLOH.hachu_no ");
                // 若番業者
                sql.Append(" LEFT JOIN T_HATTDTL EDAOH ");
                sql.Append("  ON  DTLOH.hachu_no = EDAOH.hachu_no ");
                sql.Append("  AND DTLOH.hachu_eda = EDAOH.hachu_eda ");
                sql.Append(" LEFT JOIN T_GYOSYAM GYOOH ");
                sql.Append("  ON  EDAOH.gyos_cd = GYOOH.gyos_cd ");
                sql.Append(" LEFT JOIN ( ");
                sql.Append("     SELECT hachu_no, MAX(kanryo_ymd) AS kanryo_ymd, COUNT(*) AS cnt ");
                sql.Append("     FROM T_HATTDTL ");
                sql.Append("     WHERE teishi_kb = 0 ");
                sql.Append("       AND sagyo_sb = 2 ");
                sql.Append("       AND kanryo_ymd IS NOT NULL ");
                sql.Append("     GROUP BY hachu_no ");
                sql.Append(" ) FINOH ");
                sql.Append("  ON  HED.hachu_no = FINOH.hachu_no ");

                // 有効な発注情報（退去補修のみ）
                sql.Append("WHERE HED.teishi_kb = 0 ");
                sql.Append("  AND HED.koji_sb = 1");
                //未確定のみ
                sql.Append("  AND ISNULL(HED.確定,0) <> 1");
                // 抽出条件／退去日
                cond_leaving_date_from = cond_leaving_date_from == null ? DateTime.Parse("1900/01/01") : cond_leaving_date_from;
                cond_leaving_date_to = cond_leaving_date_to == null ? DateTime.Parse("2999/12/31") : cond_leaving_date_to;

                sql.AppendFormat(" AND ((HED.taikyo_ymd IS NULL) OR (HED.taikyo_ymd BETWEEN '{0}' AND '{1}')) ", cond_leaving_date_from, cond_leaving_date_to);
                // 抽出条件／発注者
                if (cond_staf_cd != null && cond_staf_cd != "")
                {
                    sql.AppendFormat(" AND (HED.hachusya_cd = {0}) ", cond_staf_cd);
                }
                //抽出条件／物件コード
                if (cond_bukken_cd != null && cond_bukken_cd != "")
                {
                    sql.AppendFormat(" AND (HED.bukn_cd = {0}) ", cond_bukken_cd);
                }
                // 抽出条件／物件名
                if (cond_bukken_name != null && cond_bukken_name != "")
                {
                    sql.AppendFormat(" AND ( (BUK.bukn_name LIKE '%{0}%') OR (BUK.bukn_ryaku Like '%{0}%') OR (BUK.bukn_yomi LIKE '%{0}%') OR (BUK.bukn_kyomi LIKE '%{0}%') ) ", cond_bukken_name);
                }
                //ソート順（退去日）
                sql.Append("ORDER BY ISNULL(HED.taikyo_ymd, '2999/12/31'), HED.hachu_no ");

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

                foreach (DataRow dr in dt.Rows)
                {
                    model.list_report.Add(new RestrationReport
                    {
                        taikyo_ymd = dr.Field<DateTime?>("taikyo_ymd")?.ToString("yy/MM/dd"),
                        bukn_cd = dr.Field<decimal>("bukn_cd"),
                        bukn_name = dr.Field<string>("bukn_name"),
                        room_no = dr.Field<string>("room_no"),
                        HS_gyos_name = dr.Field<string>("HS_gyos_name"),
                        HS_yotei_ymd = dr.Field<DateTime?>("HS_yotei_ymd")?.ToString("yy/MM/dd"),
                        HS_kanryo_ymd = dr.Field<DateTime?>("HS_kanryo_ymd")?.ToString("yy/MM/dd"),
                        HS_seikyu_month = dr.Field<string?>("HS_seikyu_month"),
                        HS_siharai_kin = (int)dr.Field<decimal>("HS_siharai_kin"),
                        sagyo_naiyo = dr.Field<string>("sagyo_naiyo"),
                        HS_seikyu_kin = (int)dr.Field<decimal>("HS_seikyu_kin"),
                        HS_kaiyotei_ow_ymd = dr.Field<DateTime?>("HS_kaiyotei_ow_ymd")?.ToString("yy/MM/dd"),
                        HS_kaiyotei_ka_ymd = dr.Field<DateTime?>("HS_kaiyotei_ka_ymd")?.ToString("yy/MM/dd"),
                        BS_gyos_name = dr.Field<string>("BS_gyos_name"),
                        BS_yotei_ymd = dr.Field<DateTime?>("BS_yotei_ymd")?.ToString("yy/MM/dd"),
                        BS_kanryo_ymd = dr.Field<DateTime?>("BS_kanryo_ymd")?.ToString("yy/MM/dd"),
                        OH_gyos_name = dr.Field<string>("OH_gyos_name"),
                        OH_yotei_ymd = dr.Field<DateTime?>("OH_yotei_ymd")?.ToString("yy/MM/dd"),
                        OH_kanryo_ymd = dr.Field<DateTime?>("OH_kanryo_ymd")?.ToString("yy/MM/dd"),
                        nyukyo_ymd = dr.Field<DateTime?>("nyukyo_ymd")?.ToString("yy/MM/dd"),
                        counter = dr.Field<string>("BS_gyos_name"),
                        hachusya_nm = dr.Field<string>("hachusya_nm"),
                        hachu_no = dr.Field<string>("hachu_no"),
                        DTLNS_cnt = dr.Field<int>("DTLNS_cnt"),
                        FINNS_cnt = dr.Field<int>("FINNS_cnt"),
                        SEINS_cnt = dr.Field<int>("SEINS_cnt"),
                        counter_paint = dr.Field<Int16>("counter_paint"),
                        taikyo_seisou = (int)dr.Field<decimal>("taikyo_seisou"),
                        ff_seibi = (int)dr.Field<decimal>("ff_seibi"),
                    });

                }
                //セレクトボックス作成
                model.cond_staf_cd_option.Add(new SelectListItem()
                {
                    Value = "",
                    Text = "全て"
                });
                var t_staff = _context.M_STAFF.Where(x=>x.retired != 1).Select(x => new { x.staf_cd, x.staf_name }).ToList();
                for (int g = 0; g < t_staff.Count(); g++)
                {
                    var group = new SelectListItem()
                    {
                        Value = t_staff[g].staf_cd.ToString(),
                        Text = t_staff[g].staf_name
                    };
                    model.cond_staf_cd_option.Add(group);
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

    }
}
