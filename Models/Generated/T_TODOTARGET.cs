using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(todo_no), nameof(staf_cd))]
    public class T_TODOTARGET
    {
        public int todo_no { get; set; }

        public int staf_cd { get; set; }

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
