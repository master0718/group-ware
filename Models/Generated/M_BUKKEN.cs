using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class M_BUKKEN
    {
        [Key]
        public int bukken_cd { get; set; }

        [Column(TypeName = "nvarchar(60)")]
        public string? bukken_name { get; set; }
        [Column(TypeName = "nvarchar(8)")]
        public string? zip { get; set; } = "";
        [Column(TypeName = "nvarchar(40)")]
        public string? address1 { get; set; } = "";

        [Column(TypeName = "nvarchar(40)")]
        public string? address2 { get; set; } = "";

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}