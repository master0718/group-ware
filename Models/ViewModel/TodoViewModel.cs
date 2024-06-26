using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class TodoDetail
    {
        public int id {get; set;}
        [Column(TypeName ="nvarchar(64)")]
        public string? title { get; set; }
        public string? description { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string? sendUrl { get; set; }
        public int public_set {get; set;}
        public int group_set {get; set;}
        public int deadline_set {get; set;}
        public int response_status {get; set;}
        [Column(TypeName = "nvarchar(64)")]
        public string? staf_name {get; set;}
        [DataType(DataType.DateTime)]
        public DateTime? end_date { get; set; }
        public int? has_file { get; set; }
    }

    public class UserInfo
    {
        public string? userName { get; set; }
    }

    public class TodoViewModelStaff
    {
        public int staff_cd { get; set; }
        public string? staff_name { get; set; }
    }

    public class TodoFileModel
    {
        public List<T_TODO_FILE> fileList { get; set; } = new List<T_TODO_FILE>();
        public int editable = 0;
    }

    public class TodoViewModel
    {
        public List<TodoDetail>? fileList = new List<TodoDetail>();
        public List<UserInfo>? userList = new List<UserInfo>();
        public List<T_TODO>? todoList = new List<T_TODO>();
        public int id {get; set;}
        
        public string? title { get; set; }
        [Column(TypeName = "nvarchar(64)")]
        public string? description { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string? sendUrl { get; set; }
        public int public_set {get; set;}
        public int group_set {get; set;}
        public int deadline_set {get; set;}
        public int response_status {get; set;}
        [Column(TypeName = "nvarchar(64)")]
        public string? staf_name {get; set;}
        [DisplayName("添付ファイル")]
        public string? Delete_files { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? end_date { get; set; }
        public List<IFormFile> File { get; set; } = new List<IFormFile>();
        public TodoFileModel fileModel { get; set; } = new TodoFileModel();
        public string? work_dir { get; set; }
        public List<TodoViewModelStaff>? staffList = new();
        public int? has_file { get; set; }
    }

    public class TodoUpdateModel
    {

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public int public_set { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public int group_set { get; set; }
        public int deadline_set { get; set; }
        public int response_status { get; set; }
    }

}