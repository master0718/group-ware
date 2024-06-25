using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Models
{
    public class GroupMasterViewModel
    {
        public List<GroupMasterDetailViewModel> groupList = new List<GroupMasterDetailViewModel>();
    }
    public class GroupMasterDetailViewModel
    {
        [DisplayName("グループ番号")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [Range(1, 1000000000, ErrorMessage = Messages.RANGE)]
        public int group_cd { get; set; }
        public int user_count { get; set; }
        [DisplayName("グループ名")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(10, ErrorMessage = Messages.MAXLENGTH)]
        public string? group_name { get; set; }
        [DisplayName("更新者")]
        public string? update_user { get; set; }
        [DisplayName("更新日時")]
        public DateTime update_date { get; set; }
    }
}