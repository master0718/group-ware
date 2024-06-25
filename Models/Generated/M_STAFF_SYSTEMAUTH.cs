using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class M_STAFF_SYSTEMAUTH
    {
        [Key]
        public int staf_cd { get; set; }

        [Key]
        [Column(TypeName = "varchar(2)")]
        public string system_id { get; set; }

        public bool onoff { get; set; }
    }
}
