using Microsoft.EntityFrameworkCore;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(schedule_no), nameof(staf_cd))]
    public class T_SCHEDULEPEOPLE
    {
        public int schedule_no { get; set; }

        public int staf_cd { get; set; }

        public T_SCHEDULEPEOPLE(int schedule_no, int staf_cd) { 
            this.schedule_no = schedule_no;
            this.staf_cd = staf_cd;
        }
    }
}
