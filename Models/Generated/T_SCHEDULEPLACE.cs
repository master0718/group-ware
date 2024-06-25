using Microsoft.EntityFrameworkCore;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(schedule_no), nameof(place_cd))]
    public class T_SCHEDULEPLACE
    {
        public int schedule_no { get; set; }

        public int place_cd { get; set; }

        public T_SCHEDULEPLACE(int schedule_no, int place_cd)
        {
            this.schedule_no = schedule_no;
            this.place_cd = place_cd;
        }
    }
}