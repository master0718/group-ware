using Microsoft.EntityFrameworkCore;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(todo_no), nameof(group_cd))]
    public class T_TODOTARGET_GROUP
    {
        public int todo_no { get; set; }

        public int group_cd { get; set; }

        public T_TODOTARGET_GROUP(int todo_no, int group_cd)
        {
            this.todo_no = todo_no;
            this.group_cd = group_cd;
        }
    }
}
