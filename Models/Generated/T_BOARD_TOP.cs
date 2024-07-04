using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_BOARD_TOP
    {
        [Key]
        public int board_no { get; set; }
        [Key]
        public int staf_cd { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? update_date { get; set; }
    }
}