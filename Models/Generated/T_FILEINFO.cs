using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class T_FILEINFO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int file_no { get; set; }

        [Column(TypeName = "nvarchar(64)")]
        public string name { get; set; }
        [Column(TypeName = "nvarchar(10)")]
        public string? icon { get; set; } = "";
        public int size { get; set; }
        public int type { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string? path { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? create_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}