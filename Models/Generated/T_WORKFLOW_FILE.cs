using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Models
{
    public class T_WORKFLOW_FILE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int workflow_no { get; set; }

        [Key]
        public int file_no { get; set; }

        [Column(TypeName = "nvarchar(256)")]
        public string filepath { get; set; }

        [DisplayName("添付ファイル")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [StringLength(64, ErrorMessage = Messages.MAXLENGTH)]
        [Column(TypeName = "nvarchar(64)")]
        public string filename { get; set; }
    }
}