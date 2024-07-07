#pragma warning disable CS8600,CS8602,CS8604,CS8618
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace web_groupware.Models
{
    public class ReportViewModel
    {
        [DataType(DataType.Date)]
        public string? cond_date { get; set; }
        public string cond_already_read { get; set; } 
        public List<SelectListItem> list_already_read { get; set; } = new List<SelectListItem>();
        public List<List<Report>> list_report = new List<List<Report>>();
        public ReportViewModel()
        {
            list_already_read.Add(new SelectListItem { Value = "0", Text = "‘S‚Ä" });
            list_already_read.Add(new SelectListItem { Value = "1", Text = "–¢“Ç" });

        }
    }
    public class Report
    {
        public int report_no { get; set; }
        public string name { get; set; }
        public string report_date { get; set; }
        public string update_date { get; set; }
        public string message { get; set; }
        public string count { get; set; }
        public bool isMe { get; set; }

    }

}