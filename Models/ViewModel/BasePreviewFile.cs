
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class BasePreviewFile
    {
        public string dic_cd { get; set; }
        public string dir_no { get; set; }
        public string file_name { get; set; }

    }

}