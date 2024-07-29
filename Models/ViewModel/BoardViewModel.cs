using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using web_groupware.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace web_groupware.Models
{
    public class BoardModel
    {
        public int board_no { get; set; }

        [DisplayName("ステータス")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int status { get; set; }

        [DisplayName("種類")]
        public string? category_cd { get; set; }

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
        public string? applicant_cd { get; set; }

        public string? applicant_name { get; set; }

        public int already_checked { get; set; }

        [DisplayName("トップに出す")]
        public bool show_on_top { get; set; }
    }

    public class BoardCommentModel
    {
        public int board_no { get; set; }
        public int comment_no { get; set; }

        [DisplayName("内容")]
        [MaxLength(1024, ErrorMessage = Messages.MAXLENGTH)]
        public string? message { get; set; }

        [DisplayName("登緑者")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int registrant_cd { get; set; }

        public string? registrant_name { get; set; }

        [DisplayName("登緑日時")]
        public string? register_date { get; set; }

        public List<T_BOARDCOMMENT_FILE> CommentFileDetailList { get; set; } = new List<T_BOARDCOMMENT_FILE>();

    }

    public class BoardViewModelStaff
    {
        public int staff_cd { get; set; }
        public string? staff_name { get; set; }
    }

    public class BoardViewModelCategory
    {
        public int category_cd { get; set; }
        public string category_name { get; set; }
    }

    public class BoardViewModel
    {
        public string cond_already_checked { get; set; }
        public string cond_applicant { get; set; }
        public string cond_category { get; set; }
        public string? cond_keyword { get; set; }
        public List<SelectListItem> list_already_checked { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> list_applicant { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> list_category { get; set; } = new List<SelectListItem>();

        public List<BoardModel>? BoardList;

        public List<int> CountList { get; set; } = new();
        public BoardViewModel()
        {
            list_already_checked.Add(new SelectListItem { Value = "0", Text = "全て" });
            list_already_checked.Add(new SelectListItem { Value = "1", Text = "未確認" });
            list_already_checked.Add(new SelectListItem { Value = "2", Text = "確認済" });
        }
    }

    public class BoardFileModel
    {
        public List<T_BOARD_FILE> fileList { get; set; } = new List<T_BOARD_FILE>();
        public int editable = 0;
    }

    public class BoardCommentFileModel
    {
        public List<T_BOARDCOMMENT_FILE> commentFileList { get; set; } = new List<T_BOARDCOMMENT_FILE>();
    }

    public class BoardDetailViewModel: BaseViewModel
    {
        public int board_no { get; set; }

        [DisplayName("ステータス")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int status { get; set; }

        [DisplayName("種類")]
        public int? category_cd { get; set; }

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
        [DisplayName("添付ファイル")]
        public List<IFormFile> CommentFile { get; set; } = new List<IFormFile>();
        public BoardCommentFileModel commentFileModel { get; set; } = new BoardCommentFileModel();
        public string? work_dir { get; set; }
        public string? comment_work_dir { get; set; }
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

        public bool already_checked { get; set; }
        public string? check_count { get; set; }
        public List<string?> list_check_member { get; set; } = new List<string?>();

        [DisplayName("トップに出す")]
        public bool show_on_top { get; set; } = false;

        public List<BoardCommentModel>? CommentList;
        public List<BoardViewModelStaff>? StaffList = new();
        public List<BoardViewModelCategory>? CategoryList = new();

        public int commentTotalCount;

    }
}
