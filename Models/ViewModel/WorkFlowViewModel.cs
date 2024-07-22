using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;

namespace web_groupware.Models
{
    public class WorkFlowModel
    {
        public int workflow_no { get; set; }

        [DisplayName("タイトル")]
        public string? title { get; set; }

        [DisplayName("本文")]
        public string? description { get; set; }

        public int status { get; set; } // 1:作成中 | 2:申請中 | 3:承認中 | 4:否決 | 5:完了

        public int approve_result { get; set; }

        public int request_type { get; set; }

        public int requester_cd { get; set; }

        [DisplayName("承認者")]
        public string? requester_name { get; set; }

        public int approver_cd { get; set; }

        [DisplayName("承認者")]
        public string? approver_name { get; set; }

        public string update_date { get; set; }
        public string? request_date { get; set; }
        public string? approve_date { get; set; }

        public string? comment { get; set; }
    }

    public class WorkFlowViewModelStaff
    {
        public int staff_cd { get; set; }
        public string? staff_name { get; set; }
    }

    public class WorkFlowViewModelRequestType
    {
        public int request_type { get; set; } // dic_cd
        public string? request_name { get; set; }
    }

    public class WorkFlowFileModel
    {
        public List<T_WORKFLOW_FILE> fileList { get; set; } = new ();
        public int editable = 0;
    }

    public class WorkFlowViewModel
    {
        public List<WorkFlowModel>? WorkflowList = new ();
    }

    public class RequiredIfRejectAttribute : RequiredAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var model = (WorkFlowDetailViewModel)validationContext.ObjectInstance;
            if (model.is_accept == 0 && value == null)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success!;
        }
    }

    public class WorkFlowDetailViewModel
    {
        public int workflow_no { get; set; }

        [DisplayName("タイトル")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string? title { get; set; }

        [DisplayName("本文")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(64, ErrorMessage = Messages.MAXLENGTH)]
        public string? description { get; set; }

        [DisplayName("添付ファイル")]
        public List<IFormFile> File { get; set; } = new ();

        public WorkFlowFileModel fileModel { get; set; } = new ();
        public string? work_dir { get; set; }

        public string? Delete_files { get; set; }
        public string? file_nos_remove { get; set; }

        public int status { get; set; } // 2:申請中 | 3:承認中 | 4:否決

        [DisplayName("申請区分")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int request_type { get; set; }

        public string? request_type_name { get; set; }

        [DisplayName("申請者")]
        public int requester_cd { get; set; }

        public string? requester_name { get; set; } // staff_name

        [DisplayName("承認者")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int approver_cd { get; set; }

        public string? approver_name { get; set; }

        public string? approve_date { get; set; }

        [DisplayName("承認者コメント")]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string? comment { get; set; }

        public int is_accept { get; set; } // 0: reject | 1: accept

        public List<WorkFlowViewModelStaff>? staffList = new();

        public List<WorkFlowViewModelRequestType>? requestTypeList = new();
    }
}