using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class DictionaryDetail
    {
        [DisplayName("辞書コード")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [Range(1, 1000000000, ErrorMessage = Messages.RANGE)]
        public int dic_kb { get; set; }
        [DisplayName("辞書区分")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(10, ErrorMessage = Messages.MAXLENGTH)]
        public string dic_cd { get; set; }
        [DisplayName("内容")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(4000, ErrorMessage = Messages.MAXLENGTH)]
        public string? content { get; set; }
        [DisplayName("コメント")]
        public string? comment { get; set; }

        [DisplayName("更新者")]
        public string? update_user { get; set; }
        [DisplayName("更新日時")]
        public DateTime update_date { get; set; }
    }
    public class DictionaryDetailViewModel
    {
        public List<DictionaryDetail> dicList = new List<DictionaryDetail>();
    }
}