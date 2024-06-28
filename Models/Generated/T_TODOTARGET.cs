using Microsoft.EntityFrameworkCore;

namespace web_groupware.Models
{
    [PrimaryKey(nameof(todo_no), nameof(staf_cd))]
    public class T_TODOTARGET
    {
        public int todo_no { get; set; }

        public int staf_cd { get; set; }

        public T_TODOTARGET(int todo_no, int staf_cd)
        {
            this.todo_no = todo_no;
            this.staf_cd = staf_cd;
        }
    }
}
