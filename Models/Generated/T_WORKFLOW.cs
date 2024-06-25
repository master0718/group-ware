using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class T_WORKFLOW
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int id { get; set; }

        [Column(TypeName = "nvarchar(64)")]
        public string? title { get; set; }
        [Column(TypeName = "nvarchar(64)")]
        public string? description { get; set; }
        [Column(TypeName = "nvarchar(64)")]
        public string? filename { get; set; }
        [Column(TypeName = "nvarchar(10)")]
        public string? icon { get; set; } = "";
        public int size { get; set; }
        public int type { get; set; }
        public int manager_status { get; set; }
        public int approver_status { get; set; }
        [Column(TypeName = "nvarchar(64)")]
        public string? comment { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? update_user { get; set; }
        [Column(TypeName = "nvarchar(10)")]
        public string? manager { get; set; }
        [Column(TypeName = "nvarchar(10)")]
        public string? approver { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}