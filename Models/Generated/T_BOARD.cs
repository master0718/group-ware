using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class T_BOARD
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int board_no { get; set; }

        [Required(ErrorMessage = Messages.REQUIRED)]
        public int status { get; set; }
        
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public int? category_cd { get; set; }

        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string title { get; set; }

        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1024, ErrorMessage = Messages.MAXLENGTH)]
        public string content { get; set; }

        public int notifier_cd { get; set; }

        public DateTime? notify_date { get; set; }

        public int? applicant_cd { get; set; }

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