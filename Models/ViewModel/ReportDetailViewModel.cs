
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
        public string? cond_date { get; set; }
        public string cond_already_read {  get; set; }
        public bool isMe { get; set; }
        public int? report_no { get; set; }
        [DisplayName("日報年月日")]

        [Required(ErrorMessage = Messages.REQUIRED)]
        [DataType(DataType.Date)]
        public DateTime report_date { get; set; }
        [DisplayName("内容")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string message { get; set; }
        public bool already_checked { get; set; }
        public string? check_count { get; set; }
        public List<string?> list_check_member { get; set; } = new List<string?>();

        public List<CommentDetail>? list_report { get; set; }

        public CommentDetail? report { get; set; }


        [DisplayName("登録者")]
        public string? create_user { get; set; }
        [DisplayName("登録日")]
        public string? create_date { get; set; }
        [DisplayName("更新者")]
        public string? update_user { get; set; }

        [DisplayName("更新日")]
        public string? update_date { get; set; }
    }
    public class CommentDetail
    {
        public int? comment_no { get; set; }
        public string? update_user { get; set; }
        public string? update_date { get; set; }
        [DisplayName("コメント")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string message { get; set; }
        public bool already_checked_comment { get; set; }
        public string? check_count { get; set; }
        public List<string?> list_check_member { get; set; } = new List<string?>();
    }

}