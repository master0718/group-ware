using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(schedule_no), nameof(staf_cd))]
    public class T_SCHEDULEPEOPLE
    {
        public int schedule_no { get; set; }

        public int staf_cd { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime update_date { get; set; }

        public T_SCHEDULEPEOPLE(int schedule_no, int staf_cd) { 
            this.schedule_no = schedule_no;
            this.staf_cd = staf_cd;
        }
    }
}
