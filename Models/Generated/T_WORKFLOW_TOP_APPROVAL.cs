using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_WORKFLOW_TOP_APPROVAL
    {
        [Key]
        public int workflow_no { get; set; }

        [Key]
        public int approver_cd { get; set; }

        public byte approve_result { get; set; } = 0; // 0: 未読 | 1: 承認 | 2: 否決

        [DisplayName("コメント")]
        [StringLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? comment { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        [Column(TypeName = "datetime2(7)")]
        public DateTime update_date { get; set; }
    }
}