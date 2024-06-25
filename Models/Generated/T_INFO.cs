using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class T_INFO
    {
        [Key]
        public int info_cd { get; set; }
        [Column(TypeName = "nvarchar(40)")]
        [DisplayName("タイトル")]
        public string? title { get; set; }
        [Column(TypeName = "nvarchar(200)")]
        [DisplayName("コメント")]
        [StringLength(200, ErrorMessage = Messages.MAXLENGTH)]
        public string? message { get; set; } = "";
        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }

    }
}