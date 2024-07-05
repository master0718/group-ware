using Microsoft.EntityFrameworkCore;
using web_groupware.Models;

namespace web_groupware.Data
{
    public class web_groupwareContext : DbContext
    {
        public web_groupwareContext(DbContextOptions<web_groupwareContext> options)
            : base(options)
        {
        }

        public DbSet<T_INFO> T_INFO { get; set; }
        public DbSet<T_INFO_FILE> T_INFO_FILE { get; set; }
        public DbSet<M_STAFF> M_STAFF { get; set; }
        public DbSet<M_STAFF_AUTH> M_STAFF_AUTH { get; set; }
        public DbSet<M_STAFF_SYSTEMAUTH> M_STAFF_SYSTEMAUTH { get; set; }
        public DbSet<T_HOLIDAY> T_HOLIDAY { get; set; }
        public DbSet<M_GROUP> M_GROUP { get; set; }
        public DbSet<T_GROUPSTAFF> T_GROUPSTAFF { get; set; }
        public DbSet<M_BUKKEN> M_BUKKEN { get; set; }
        public DbSet<T_BUKKENCOMMENT> T_BUKKENCOMMENT { get; set; }
        public DbSet<T_BUKKENCOMMENT_FILE> T_BUKKENCOMMENT_FILE { get; set; }

        public DbSet<T_NO> T_NO { get; set; }
        public DbSet<T_REPORT> T_REPORT { get; set; }
        public DbSet<T_INFO_PERSONAL> T_INFO_PERSONAL { get; set; }
        public DbSet<T_REPORTCOMMENT> T_REPORTCOMMENT { get; set; }
        public DbSet<R_RESTORATION_REPORT> R_RESTORATION_REPORT { get; set; }
        public DbSet<T_FILEINFO> T_FILEINFO { get; set; }
        public DbSet<T_ATTENDANCE_DATE> T_ATTENDANCE_DATE { get; set; }
        public DbSet<T_WORKFLOW> T_WORKFLOW { get; set; }
        public DbSet<T_WORKFLOW_FILE> T_WORKFLOW_FILE { get; set; }
        public DbSet<M_DIC> M_DIC { get; set; }
        public DbSet<T_PLACEM> T_PLACEM { get; set; }
        public DbSet<M_SCHEDULE_TYPE> M_SCHEDULE_TYPE { get; set; }
        public DbSet<M_SYSTEM> M_SYSTEM { get; set; }

        public DbSet<T_MEMO> T_MEMO { get; set; }
        public DbSet<T_TODO> T_TODO { get; set; }
        public DbSet<T_TODO_FILE>? T_TODO_FILE { get; set; }
        public DbSet<T_TODOTARGET>? T_TODOTARGET { get; set; }
        public DbSet<T_TODOTARGET_GROUP>? T_TODOTARGET_GROUP { get; set; } 

        public DbSet<T_SCHEDULE>? T_SCHEDULE { get; set; }
        public DbSet<T_SCHEDULE_FILE>? T_SCHEDULE_FILE { get; set; }
        public DbSet<T_SCHEDULEPEOPLE> T_SCHEDULEPEOPLE { get; set; }
        public DbSet<T_SCHEDULEGROUP> T_SCHEDULEGROUP { get; set; }
        public DbSet<T_SCHEDULEPLACE> T_SCHEDULEPLACE { get; set; }
        public DbSet<T_SCHEDULE_REPETITION> T_SCHEDULE_REPETITION { get; set; }

        public DbSet<T_BOARD>? T_BOARD { get; set; }
        public DbSet<T_BOARD_TOP>? T_BOARD_TOP { get; set; }
        public DbSet<T_BOARD_FILE>? T_BOARD_FILE { get; set; }
        public DbSet<T_BOARDCOMMENT>? T_BOARDCOMMENT { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<T_INFO>()
                .HasKey(c => new { c.info_cd });

            modelBuilder.Entity<M_BUKKEN>()
                .HasKey(c => new { c.bukken_cd });

            modelBuilder.Entity<T_BUKKENCOMMENT>()
                .HasKey(c => new { c.bukn_cd,c.comment_no });
            modelBuilder.Entity<M_DIC>()
                .HasKey(c => new { c.dic_kb, c.dic_cd });

            modelBuilder.Entity<T_FILEINFO>()
                .HasKey(c => new { c.file_no });

            modelBuilder.Entity<T_WORKFLOW>()
                .HasKey(c => new { c.id });
            modelBuilder.Entity<T_WORKFLOW_FILE>()
                .HasKey(c => new { c.workflow_no, c.file_no });

            modelBuilder.Entity<T_MEMO>()
                .HasKey(c => new { c.memo_no });
            modelBuilder.Entity<T_GROUPSTAFF>()
                .HasKey(c => new { c.staf_cd, c.group_cd });
            modelBuilder.Entity<T_TODO>()
                .HasKey(k => new { k.todo_no });
            modelBuilder.Entity<T_TODO_FILE>()
                .HasKey(c => new { c.todo_no, c.file_no });
            modelBuilder.Entity<T_ATTENDANCE_DATE>()
                .HasKey(k => new { k.id });
            modelBuilder.Entity<T_HOLIDAY>()
                .HasNoKey();
            modelBuilder.Entity<M_STAFF_SYSTEMAUTH>()
                .HasKey(c => new { c.staf_cd, c.system_id });
            modelBuilder.Entity<M_SYSTEM>()
                .HasNoKey();
            modelBuilder.Entity<T_INFO_PERSONAL>()
                .HasKey(c => new { c.parent_id, c.first_no, c.second_no, c.third_no, c.staf_cd });
            modelBuilder.Entity<T_BUKKENCOMMENT_FILE>()
                .HasKey(c => new { c.bukn_cd, c.comment_no, c.file_no });



            modelBuilder.Entity<T_SCHEDULE>()
                .HasKey(c => new { c.schedule_no });
            modelBuilder.Entity<T_SCHEDULE_FILE>()
                .HasKey(c => new { c.schedule_no, c.file_no });
            modelBuilder.Entity<T_SCHEDULEPEOPLE>()
                .HasKey(t => new { t.schedule_no, t.staf_cd });
            modelBuilder.Entity<T_SCHEDULEGROUP>()
                .HasKey(t => new { t.schedule_no, t.group_cd });
            modelBuilder.Entity<T_SCHEDULEPLACE>()
                .HasKey(t => new { t.schedule_no, t.place_cd });
            modelBuilder.Entity<T_SCHEDULE_REPETITION>()
                .HasKey(t => new { t.schedule_no });

            modelBuilder.Entity<T_BOARD>()
                .HasKey(c => new { c.board_no });
            modelBuilder.Entity<T_BOARD_TOP>()
                .HasKey(c => new { c.board_no, c.staf_cd });
            modelBuilder.Entity<T_BOARD_FILE>()
                .HasKey(c => new { c.board_no, c.file_no });
            modelBuilder.Entity<T_BOARDCOMMENT>()
                .HasKey(c => new { c.board_no, c.comment_no });
        }
    }
}
