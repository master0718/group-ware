using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace web_groupware.Models
{
    public class M_PLACE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int place_cd { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public string? place_name { get; set; }

        public int sort { get; set; }
        public bool duplicate { get; set; }

        public M_PLACE() { }
    }
}
