using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class R_RESTORATION_REPORT
    {
        [Key]
        public int hachu_no { get; set; }
        [Column(TypeName = "nvarchar(30)")]
        public string bukken_name { get; set; }
        public int room_no { get; set; }
        [DataType(DataType.Date)]
        public DateTime leaving_date { get; set; } 
        public int leaving_staffcd { get; set; }
        [DataType(DataType.Date)]
        public DateTime restoration_date { get; set; }
    }
}