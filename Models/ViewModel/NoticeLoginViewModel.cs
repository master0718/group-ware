using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
namespace web_groupware.Models
{
    public class NoticeLoginViewModel
    {
        [DisplayName("�^�C�g��")]
        [StringLength(40, ErrorMessage = Messages.MAXLENGTH)]
        public string? title { get; set; }
        [DisplayName("�R�����g")]
        [StringLength(200, ErrorMessage = Messages.MAXLENGTH)]
        public string? message { get; set; } = "";
    }
}