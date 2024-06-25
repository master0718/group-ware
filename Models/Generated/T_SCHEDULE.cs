using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace web_groupware.Models
{
    public class T_SCHEDULE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int schedule_no { get; set; }
        [Required(ErrorMessage = "必須項目です。")]
        public int schedule_type { get; set; }

        public bool allday { get; set; } = false;

        //[Required(ErrorMessage = "必須項目です。")]
        [Column(TypeName = "datetime2(7)")]
        public DateTime? start_datetime { get; set; }
    
        //[Required(ErrorMessage = "必須項目です。")]
        [Column(TypeName = "datetime2(7)")]
        public DateTime? end_datetime { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string? title { get; set; }

        [Column(TypeName = "nvarchar(2000)")]
        public string? memo { get; set; }

        public bool is_private { get; set; } = false;

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
