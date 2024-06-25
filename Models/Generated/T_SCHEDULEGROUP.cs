using Microsoft.EntityFrameworkCore;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(schedule_no), nameof(group_cd))]
    public class T_SCHEDULEGROUP
    {
        public int schedule_no { get; set; }

        public int group_cd { get; set; }

        public T_SCHEDULEGROUP(int schedule_no, int group_cd) { 
            this.schedule_no = schedule_no;
            this.group_cd = group_cd;
        }
    }
}
