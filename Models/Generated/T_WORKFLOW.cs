using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_WORKFLOW
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int workflow_no { get; set; }

        [Column(TypeName = "nvarchar(64)")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string title { get; set; }

        [Column(TypeName = "nvarchar(64)")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string description { get; set; }

        public byte status { get; set; } // 1:作成中 | 2:申請中 | 3:承認中 | 4:否決 | 5:完了

        public int request_type { get; set; }

        public int requester_cd { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime request_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }
    }
}