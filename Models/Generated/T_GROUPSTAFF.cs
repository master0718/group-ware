using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class T_GROUPSTAFF
    {
        [Key]
        public int staf_cd { get; set; }
        [ForeignKey(nameof(staf_cd))]
        public virtual M_STAFF? staff { get; set; }

        [Key]
        public int group_cd { get; set; }
        [ForeignKey(nameof(group_cd))]
        public virtual T_GROUPM? group { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}
