using System.Collections.Generic;
using web_groupware.Models;
namespace web_groupware.Models
{
    public class Attendance_StaffAndYearModel
    {
        public List<M_STAFF> StaffMembers { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }
}
