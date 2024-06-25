using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class RestrationReportViewModel
    {
        [DataType(DataType.Date)]
        public DateTime? cond_leaving_date_from { get; set; }
        [DataType(DataType.Date)]
        public DateTime? cond_leaving_date_to { get; set; }
        [MaxLength(10, ErrorMessage = Messages.MAXLENGTH)]
        public string? cond_staf_cd { get; set; }
        public List<SelectListItem> cond_staf_cd_option { get; set; } = new List<SelectListItem>();
        public string? cond_bukken_cd { get; set; }
        public string? cond_bukken_name { get; set; }



        public List<RestrationReport> list_report = new List<RestrationReport>();
    }
    public class RestrationReport
    {
        public string taikyo_ymd { get; set; }
        public decimal bukn_cd { get; set; }
        public string bukn_name { get; set; }
        public string room_no { get; set; }
        public string HS_gyos_name { get; set; }

        public string HS_yotei_ymd { get; set; }
        public string HS_kanryo_ymd { get; set; }
        public string HS_seikyu_month { get; set; }
        public decimal HS_siharai_kin {  get; set; }
        public string sagyo_naiyo { get; set; }
        public decimal HS_seikyu_kin { get; set; }
        public string HS_kaiyotei_ow_ymd { get; set; }
        public string HS_kaiyotei_ka_ymd { get; set; }
        public string BS_gyos_name { get; set; }
        public string BS_yotei_ymd { get; set; }
        public string BS_kanryo_ymd { get; set; }

        public string OH_gyos_name { get; set; }
        public string OH_yotei_ymd { get; set; }
        public string OH_kanryo_ymd { get; set; }
        public string nyukyo_ymd { get; set; }

        public string counter { get; set; }
        public string hachusya_nm { get; set; }
        public string hachu_no { get; set; }
        public int DTLNS_cnt { get; set; }
        public int FINNS_cnt { get; set; }
        public int SEINS_cnt { get; set; }
        public int counter_paint { get; set; }
        public decimal taikyo_seisou { get; set; }
        public decimal ff_seibi { get; set; }












    }
}