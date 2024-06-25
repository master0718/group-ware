using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class M_STAFF
    {
        [Key]
        [DisplayName("社員番号")]
        public int staf_cd { get; set; }

        [Column(TypeName = "varchar(20)")]
        [DisplayName("パスワード")]
        public string? password { get; set; }

        [DisplayName("社員名名")]
        [Column(TypeName = "nvarchar(10)")]
        public string? staf_name { get; set; }

        [Column(TypeName = "varchar(50)")]
        [DisplayName("メールアドレス")]
        public string? mail {  get; set; }

        [DisplayName("更新者")]
        [Column(TypeName = "varchar(10)")]
        public string? updid { get; set; }

        [DisplayName("更新日時")]
        [Column(TypeName = "datetime2(7)")]
        public DateTime? upddate { get; set; }

        [DisplayName("退職")]
        [Column("退職", TypeName = "smallint")]
        public int? retired { get; set; }
    }
}
