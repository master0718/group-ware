using web_groupware.Models;

namespace web_groupware.Models
{
    public class AddGEventViewModel
    {
        public List<DateTimeRange> Datetimes { get; set; }
        public T_SCHEDULE Schedule { get; set; }
        public List<People>? People { get; set; }
        public List<Places>? Places { get; set; }
    }

    public class DateTimeRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class Places
    {
        public int place_cd { get; set; }
    }

    public class People
    {
        public int staf_cd { get; set; }
    }
}
