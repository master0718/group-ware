using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Models
{
    public class T_GROUPM
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int group_cd { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? group_name { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? update_user { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime update_date { get; set; }

    }
}
