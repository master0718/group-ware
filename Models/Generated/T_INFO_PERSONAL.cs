using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_INFO_PERSONAL
    {
        [Key]

        public int info_personal_no { get; set; }
        public int parent_id { get; set; }
        public int first_no { get; set; }
        public int second_no { get; set; }
        public int third_no { get; set; }
        public int staf_cd { get; set; }
        [ForeignKey(nameof(staf_cd))]
        public virtual M_STAFF? staff { get; set; }
        public bool already_read { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string url { get; set; }
        public bool added { get; set; }

        public string create_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }

        public string update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}