
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class ReportDetailViewModel
    {
        public int? mode { get; set; }
        [DataType(DataType.Date)]
        public DateTime? cond_date { get; set; }
        public string cond_already_read {  get; set; }
        public int? report_no { get; set; }
        [DisplayName("日報年月日")]

        [Required(ErrorMessage = Messages.REQUIRED)]
        [DataType(DataType.Date)]
        public DateTime report_date { get; set; }
        [DisplayName("内容")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string message { get; set; }
        [DisplayName("周知範囲")]
        //public List<string> list_selected_staf_cd_report { get; set; }=new List<string>();
        //public List<SelectListItem> list_staf_cd_report { get; set; }=new List<SelectListItem>();
        public bool already_read { get; set; }
        public int? already_read_commment_no { get; set; }
        public List<ReportDetail>? list_report { get; set; }

        public ReportDetail? report { get; set; }


        [DisplayName("登録者")]
        public string? create_user { get; set; }
        [DisplayName("登録日")]
        public string? create_date { get; set; }
        [DisplayName("更新者")]
        public string? update_user { get; set; }

        [DisplayName("更新日")]
        public string? update_date { get; set; }
    }
    public class ReportDetail
    {
        public int? comment_no { get; set; }
        public string? update_user { get; set; }
        public string? update_date { get; set; }
        [DisplayName("コメント")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string message { get; set; }
        public List<string> list_selected_staf_cd_comment { get; set; } = new List<string>();
        public List<SelectListItem> list_staf_cd_comment { get; set; } = new List<SelectListItem>();
        public bool already_read_comment { get; set; }
    }
}