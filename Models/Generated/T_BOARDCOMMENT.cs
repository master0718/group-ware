using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class T_BOARDCOMMENT
    {
        [Key, Column(Order = 0)]
        public int board_no { get; set; }

        [Key, Column(Order = 1)]
        public int comment_no { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string message { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}