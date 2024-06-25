
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class BaseDialogStaffViewModel
    {
        public string Selected_group_cd { get; set; }
        public List<SelectListItem> List_group_cd { get; set; }=new List<SelectListItem>();
        public List<BaseDialogStaffCheckBoxModel> List_staf_cd { get; set; }=new List<BaseDialogStaffCheckBoxModel>();
    }
    public class BaseDialogStaffCheckBoxModel
    {
        public int Staf_cd { get; set; }
        public bool Is_checked { get; set; }
        public string Staf_name { get; set; }
    }

}