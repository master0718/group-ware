using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_REPORTCOMMENT
    {
        [Key]
        public int report_no { get; set; }
        public int comment_no { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string? message { get; set; }
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