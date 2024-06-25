#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class T_HOLIDAY
    {
        public DateTime holiday_date { get; set; }
        public string holiday_name { get; set; }
        public string update_user { get; set; }
        public DateTime update_date { get; set; }
    }
}