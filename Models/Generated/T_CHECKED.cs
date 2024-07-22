using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_CHECKED
    {
        [Key]
        public int check_no { get; set; }
        public int? parent_id { get; set; }
        public int? first_no { get; set; }
        public int? second_no { get; set; }
        public int? third_no { get; set; }
        public int staf_cd { get; set; }
        public string create_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }
        public string update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}