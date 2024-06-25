using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class EmployeeGroupDetail
    {
        [DisplayName("従業員番号")]
        public int staf_cd { get; set; }
        [DisplayName("グループ番号")]
        public int group_cd { get; set; }
        [DisplayName("グループ名")]
        public string? group_name { get; set; }
        [DisplayName("スタッフ名")]
        public string staf_name { get; set; }

        [DisplayName("更新者")]
        public string update_user { get; set; }
        [DisplayName("更新日時")]
        public DateTime update_date { get; set; }
    }
    public class EmployeeGroupDetailViewModel
    {
        public List<EmployeeGroupDetail> empGroupList = new List<EmployeeGroupDetail>();
    }
}