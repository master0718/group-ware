using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;

#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class M_SCHEDULE_TYPE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int schedule_type { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? schedule_typename { get; set; } 
        [Column(TypeName = "char(7)")]
        public string color { get; set; }
        [Column(TypeName = "char(7)")]
        public string colorbk { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}