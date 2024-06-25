using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using web_groupware.Utilities;

namespace web_groupware.Models
{
    public class LoginViewModel
    {
        
        [Required(ErrorMessage = Messages.REQUIRED)]
        [EmailAddress(ErrorMessage = Messages.EMAILADDRESS)]
        [Column(TypeName = "varchar(50)")]
        [DisplayName("メールアドレス")]
        [MaxLength(50)]
        public string mail { get; set; }

        [Column(TypeName = "varchar(20)")]
        [DisplayName("パスワード")]
        [MaxLength(20)]
        [DataType(DataType.Password)]
        public string? password { get; set; }
        [DisplayName("次回から自動ログイン")]
        public bool remember { get; set; }


        public string? title { get; set; }
        [Column(TypeName = "nvarchar(200)")]
        public string? message { get; set; } = "";


        public LoginViewModel()
        {
            remember = false;
            mail = string.Empty;
        }
    }
}
