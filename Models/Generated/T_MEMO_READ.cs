using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_MEMO_READ
    {
        [Key]
        public int memo_no { get; set; }
        [Key]
        public int staff_cd { get; set; }
        [ForeignKey(nameof(staff_cd))]
        public virtual M_STAFF? staff { get; set; }
        public bool read_flag { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}