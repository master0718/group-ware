using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8602,CS8604,CS8618
namespace web_groupware.Models
{
    public class BukkenMemoViewModel
    {
        [MaxLength(60, ErrorMessage = Messages.MAXLENGTH)]
        public string? cond_bukken_name { get; set; }
        public string? cond_contract_status { get; set; } = "0";
        public List<SelectListItem> cond__list_contract_status { get; set; } = new List<SelectListItem>();
        public BukkenMemoViewModel()
        {
            cond__list_contract_status.Add(new SelectListItem { Value = "", Text = "‘S‚Ä" });
            cond__list_contract_status.Add(new SelectListItem { Value = "0", Text = "Œ_–ñ’†" });
        }

        public List<BukkenMemo> list_bukken = new List<BukkenMemo>();
    }
    public class BukkenMemo
    {
        public decimal bukn_cd { get; set; }
        public string? bukken_name { get; set; }
        public string update_user { get; set; }
        public string update_date { get; set; }
        public string count { get; set; }
    }

}