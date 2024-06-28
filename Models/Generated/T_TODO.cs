using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class T_TODO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int id {get; set;}
        [Column(TypeName ="nvarchar(64)")]
        public string? title { get; set; }
        public string? description { get; set; }
        [Column(TypeName = "nvarchar(1000)")]
        public string? sendUrl { get; set; }
        public int public_set {get; set;}
        public int group_set {get; set;}
        public int deadline_set {get; set;}
        public int response_status {get; set;}
        [Column(TypeName = "nvarchar(64)")]
        public string? staf_name {get; set;}
        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? deadline_date { get; set; }
        public int? has_file { get; set; }
        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }
    }
}