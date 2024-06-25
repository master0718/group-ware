using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class FileDetail
    {
        public int file_no { get; set; }
        public string? name { get; set; }
        public string? icon { get; set; }
        public int size { get; set; }
        public int type { get; set; }

        public string? update_user { get; set; }
        public DateTime update_date { get; set; }
    }
    public class FileDetailViewModel
    {
        public List<FileDetail> fileList = new List<FileDetail>();
    }
}