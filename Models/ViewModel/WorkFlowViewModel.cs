using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_groupware.Models
{
    public class WorkFlowDetail
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? filename { get; set; }
        public string? icon { get; set; }
        public int size { get; set; }
        public int type { get; set; }

        public string? update_user { get; set; }
        public int manager_status { get; set; }
        public int approver_status { get; set; }
        public string? comment { get; set; }
        public DateTime update_date { get; set; }
        public string? Delete_files { get; set; }
        public List<IFormFile> File { get; set; } = new List<IFormFile>();
        public WorkFlowFileModel fileModel { get; set; } = new WorkFlowFileModel();
        public string? work_dir { get; set; }
        public List<WorkFlowViewModelStaff>? staffList = new();
    }
    public class WorkFlowViewModelStaff
    {
        public int staff_cd { get; set; }
        public string? staff_name { get; set; }
    }

    public class WorkFlowFileModel
    {
        public List<T_WORKFLOW_FILE> fileList { get; set; } = new List<T_WORKFLOW_FILE>();
        public int editable = 0;
    }
    public class WorkFlowViewModel
    {
        public List<WorkFlowDetail>? fileList = new List<WorkFlowDetail>();
    }
    public class WorkFlowUpdateModel
    {

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public string description { get; set; }
        public string title { get; set; }
    }
}