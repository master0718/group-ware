using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Models
{
    public class M_DIC
    {
        [Key]
        public int dic_kb { get; set; }
        [Key]
        public string dic_cd { get; set; }
        public string content { get; set; }
        public string? comment { get; set; }
        public string? update_user { get; set; }
        public DateTime update_date { get; set; }
    }
}