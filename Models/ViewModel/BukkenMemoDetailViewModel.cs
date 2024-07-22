using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class BukkenMemoDetailViewModel
    {
        public string? cond_bukken_name {  get; set; }
        public int? already_read_comment_no { get; set; }

        public int bukn_cd { get; set; }
        public string? bukken_name { get; set; }
        public string? bukken_nameWithCode { get; set; }
        public string? zip { get; set; }
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public List<BukkenMemoDetail> list_detail = new List<BukkenMemoDetail>();
        [DisplayName("コメント")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string? message_new { get; set; }
        [DisplayName("ファイル")]
        public List<IFormFile> File { get; set; } = new List<IFormFile>();
        public List<T_BUKKENCOMMENT_FILE> List_T_BUKKENCOMMENT_FILE { get; set; } = new List<T_BUKKENCOMMENT_FILE>();
        public string dic_cd { get; set; } = DIC_KB_700_DIRECTORY.BUKKENCOMMENT_FILE;
        public string dir_no { get; set; }
        public string work_dir { get; set; }

        public string? Delete_files { get; set; }

    }
    public class BukkenMemoDetail
    {
        public int? comment_no { get; set; }
        public string update_user { get; set; }
        public string update_date { get; set; }
        public string? message { get; set; }
        public bool already_checked_comment {  get; set; }
        public string check_count {  get; set; }
        public List<string?> list_check_member { get; set; }=new List<string?>();
        public List<T_BUKKENCOMMENT_FILE> List_T_BUKKENCOMMENT_FILE_ADDED { get; set; } = new List<T_BUKKENCOMMENT_FILE>();

    }

}