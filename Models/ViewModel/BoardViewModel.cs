using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using web_groupware.Utilities;

namespace web_groupware.Models
{
    public class BoardModel
    {
        public int board_no { get; set; }

        [DisplayName("status")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int status { get; set; }

        [DisplayName("タイトル")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string title { get; set; } = string.Empty;

        [DisplayName("内容")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1024, ErrorMessage = Messages.MAXLENGTH)]
        public string content { get; set; } = string.Empty;

        [DisplayName("登緑者")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public string? registrant_name { get; set; }

        [DisplayName("登緑日時")]
        public string register_date { get; set; } = DateTime.Now.ToString("yyyy-HH-MM hh:mm");

        [DisplayName("通知対象者")]
        public int notifier_cd { get; set; }

        public string? notifier_name { get; set; }

        [DisplayName("通知日時")]
        public string? notify_date { get; set; }

        [DisplayName("担当者")]
        public int? applicant_cd { get; set; }

        public string? applicant_name { get; set; }
    }

    public class BoardCommentModel
    {
        public int board_no { get; set; }
        public int comment_no { get; set; }

        [DisplayName("内容")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1024, ErrorMessage = Messages.MAXLENGTH)]
        public string message { get; set; } = string.Empty;

        [DisplayName("登緑者")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int registrant_cd { get; set; }

        public string? registrant_name { get; set; }

        [DisplayName("登緑日時")]
        public string? register_date { get; set; }
    }

    public class BoardViewModelStaff
    {
        public int staff_cd { get; set; }
        public string? staff_name { get; set; }
    }

    public class BoardViewModel
    {
        public List<BoardModel>? BoardList;
        public List<BoardViewModelStaff>? staffList = new();
    }

    public class BoardFileModel
    {
        public List<T_BOARD_FILE> fileList { get; set; } = new List<T_BOARD_FILE>();
        public int editable = 0;
    }

    public class BoardDetailViewModel
    {
        public int board_no { get; set; }

        [DisplayName("status")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int status { get; set; }

        [DisplayName("タイトル")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string title { get; set; } = string.Empty;

        [DisplayName("内容")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1024, ErrorMessage = Messages.MAXLENGTH)]
        public string content { get; set; } = string.Empty;

        [DisplayName("添付ファイル")]
        public List<IFormFile> File { get; set; } = new List<IFormFile>();
        public BoardFileModel fileModel { get; set; } = new BoardFileModel();
        public string? work_dir { get; set; }

        public string? Delete_files { get; set; }
        public string? file_nos_remove { get; set; }

        [DisplayName("登緑者")]
        public string? registrant_name { get; set; }

        [DisplayName("登緑日時")]
        public string register_date { get; set; } = DateTime.Now.ToString("yyyy-HH-MM hh:mm");

        [DisplayName("通知対象者")]
        public int notifier_cd { get; set; }

        public string? notifier_name { get; set; }

        [DisplayName("通知日時")]
        public string? notify_date { get; set; }

        [DisplayName("担当者")]
        public int? applicant_cd { get; set; }
        public string? applicant_name { get; set; }

        public List<BoardCommentModel>? CommentList;
        public List<BoardViewModelStaff>? staffList = new();

        public int commentTotalCount;
    }
}
