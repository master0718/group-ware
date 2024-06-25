using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class SimpleScheduleModel
    {
        public int schedule_type { get; set; }

        public bool allday { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? start_datetime { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? end_datetime { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? title { get; set; }

        [Column(TypeName = "nvarchar(200)")]
        public string? memo { get; set; }
    }
}
