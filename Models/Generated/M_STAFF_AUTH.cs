using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class M_STAFF_AUTH
    {
        [Key]
        [DisplayName("社員番号")]
        public int staf_cd { get; set; }

        [DisplayName("管理者権限")]
        public int auth_admin { get; set; } = 0;
        [DisplayName("承認者権限")]
        public int workflow_auth { get; set; } = 0;

        [DisplayName("更新者")]
        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DisplayName("更新日時")]
        [Column(TypeName = "datetime2(7)")]
        public DateTime update_date { get; set; }
    }
}
