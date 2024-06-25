#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using web_groupware.Utilities;

namespace web_groupware.Models
{
    public class MemoModel
    {
        public int memo_no { get; set; }
        public string? create_date { get; set; }
        public int state { get; set; }
        public int receiver_type { get; set; }
        public int receiver_cd { get; set; }
        public string? receiver_name { get; set; }
        public int? applicant_type { get; set; }
        public int? applicant_cd { get; set; }
        public string? applicant_name { get; set; }
        public string? comment_no { get; set; }
        public string? phone { get; set; }
        public string? content { get; set; }
        public string? sender_name { get; set; }
        public bool is_editable { get; set; }
        public string? readers { get; set; }
        public string? working_msg { get; set; }
        public string? finish_msg { get; set; }
    }
    public class MemoComment
    {
        public string comment_no { get; set; }
        public string? comment { get; set; }
    }
    public class MemoViewModelStaff
    {
        public int staff_cd { get; set; }
        public string? staff_name { get; set; }
    }
    public class MemoViewModelGroup
    {
        public int group_cd { get; set; }
        public string? group_name { get; set; }
    }

    public class MemoViewModel
    {
        public List<MemoModel>? memoList = new();
        public List<MemoViewModelStaff>? staffList = new();
        public List<MemoViewModelGroup>? groupList = new();
        public List<MemoComment>? commentList = new();
        public int selectedState = 0;
        public int selectedUser = 0;
        public bool isSent = true;
    }

    public class MemoDetailViewModel
    {
        public int memo_no { get; set; }
        public int state { get; set; }
        public int receiver_type { get; set; }

        [DisplayName("宛先")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int receiver_cd { get; set; }

        public int? applicant_type { get; set; }

        [DisplayName("依頼主")]
        public int? applicant_cd { get; set; }

        [DisplayName("用件")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public string comment_no { get; set; } = string.Empty;

        [DisplayName("電話番号")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(20, ErrorMessage = Messages.MAXLENGTH)]
        public string phone { get; set; } = string.Empty;

        [DisplayName("伝言")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(20, ErrorMessage = Messages.MAXLENGTH)]
        public string content { get; set; } = string.Empty;

        [DisplayName("既読者")]
        public string? readers { get; set; }

        [DisplayName("対応します")]
        public int working { get; set; }

        [DisplayName("処理済\r\n")]
        public int finish { get; set; }
        public string? working_msg { get; set; }
        public string? finish_msg { get; set; }

        public List<MemoViewModelGroup>? groupList = new List<MemoViewModelGroup>();
        public List<MemoViewModelStaff>? staffList = new List<MemoViewModelStaff>();
        public List<MemoComment>? commentList = new List<MemoComment>();
    }

    public class CreateUpdateMemoRequest
    {
        public int memo_no { get; set; }
        public int receiver_type { get; set; }
        public int receiver_cd { get; set; }
        public string comment_no { get; set; }
        public string phone { get; set; }
        public string content { get; set; }
        public int working { get; set; }
        public int finish { get; set; }
    }
    public class UpdateMemoStateRequest
    {
        public int memo_no { get; set; }
        public int state { get; set; }
    }
}