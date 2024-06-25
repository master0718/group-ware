using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class T_REPORT
    {
        [Key]
        public int report_no { get; set; }
        [DataType(DataType.Date)]
        public DateTime report_date { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string message { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime update_date { get; set; }
    }
}