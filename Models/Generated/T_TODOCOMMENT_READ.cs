using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    [PrimaryKey(nameof(todo_no), nameof(comment_no), nameof(staf_cd))]
    public class T_TODOCOMMENT_READ
    {
        public int todo_no { get; set; }
        public int comment_no { get; set; }
        public int staf_cd { get; set; }
        public bool alreadyread_flg { get; set; } 
        [Column(TypeName = "varchar(10)")]
        public string update_user { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime update_date { get; set; }
    }
}