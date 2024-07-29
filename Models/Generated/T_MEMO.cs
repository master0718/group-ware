using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;

#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class T_MEMO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int memo_no { get; set; }

        public int state { get; set; }
        public int receiver_type { get; set; }
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int receiver_cd { get; set; }

        public int? applicant_type { get; set; }
        public int? applicant_cd { get; set; }

        [Required(ErrorMessage = Messages.REQUIRED)]
        [Column(TypeName = "varchar(10)")]
        public string comment_no { get; set; } 

        [Column(TypeName = "varchar(20)")]
        public string? phone { get; set; }

        [DisplayName("伝言")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [StringLength(255, ErrorMessage = Messages.MAXLENGTH)]
        [Column(TypeName = "nvarchar(255)")]
        public string? content { get; set; }
        public int sender_cd { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string create_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime create_date { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}