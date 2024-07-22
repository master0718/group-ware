using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class T_SCHEDULE_REPETITION
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int schedule_no { get; set; }

        public byte type { get; set; }

        public byte? every_on { get; set; }

        [Column(TypeName = "nvarchar(5)")]
        public string? time_from { get; set; }

        [Column(TypeName = "nvarchar(5)")]
        public string? time_to { get; set; }

        [Column(TypeName = "date")]
        public DateTime? date_from { get; set; }

        [Column(TypeName = "date")]
        public DateTime? date_to { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime update_date { get; set; }        
    }
}