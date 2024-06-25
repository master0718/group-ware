using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Models
{
    public class T_INFO_FILE
    {
        [Key]
        public int? file_no { get; set; }
        public int? info_cd { get; set; }
        public string? fileName { get; set; } 
        public string? fullPath { get; set; }
        public string? create_user { get; set; }
        public DateTime? create_date { get; set; }
        public string? update_user { get; set; }
        public DateTime? update_date { get; set; }
    }
}