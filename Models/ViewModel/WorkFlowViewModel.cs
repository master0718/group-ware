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

        public int is_top_approver { get; set; } = -1; // -1: none | 0: Approver & Me | 1: Top Approver & Me

        public int my_approval_result { get; set; } = 0;

        public int request_type { get; set; }
        public int requester_cd { get; set; }
        public string? requester_name { get; set; }
        //public string? request_date { get; set; }

        public ApproverModel? approver1 { get; set; }
        public ApproverModel? approver2 { get; set; }
        public ApproverModel top_approver { get; set; } = new ();

        public string update_date { get; set; }
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
        public int selectedStatus = 0;
        public string? keyword;
    }

    public class RequiredIfRejectAttribute : RequiredAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var model = (ApproveDetailViewModel) validationContext.ObjectInstance;
            if (model.is_accept == 0 && value == null)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success!;
        }
    }

    //public class RequiredIfApprove1SelectedAttribute : CompareAttribute
    /*public class RequiredIfApprove1SelectedAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public RequiredIfApprove1SelectedAttribute(string otherProperty) : base(otherProperty) {
            _comparisonProperty = otherProperty;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty) ?? throw new ArgumentException("Property with this name not found");
            var currValue = (int)(value ?? 0);
            var comparisonValue = (int)(property.GetValue(validationContext.ObjectInstance) ?? 0);

            //if (currValue == 0 && comparisonValue != 0)
            if (comparisonValue == 2)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success!;
        }

        *//*public override bool IsValid(object? value)
        {
            return true;
        }*//*
    }*/

    /*public class RequiredIfApprove1SelectedAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return false;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return new ValidationResult(ErrorMessage);
        }
    }*/

    public class WorkFlowDetailViewModel: BaseViewModel
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
        public int request_type { get; set; }

        public string? request_type_name { get; set; }

        [DisplayName("申請者")]
        public int requester_cd { get; set; }

        public string? requester_name { get; set; } // staff_name

        [DisplayName("承認者1")]
        public int? approver_cd1 { get; set; }

        [DisplayName("承認者2")]
        //[RequiredIfApprove1Selected("approver_cd1", ErrorMessage = Messages.WORKFLOW_APPROVAL_APPROVER2_REQUIRED)]
        //[RequiredIfApprove1Selected(ErrorMessage = Messages.WORKFLOW_APPROVAL_APPROVER2_REQUIRED)]
        public int? approver_cd2 { get; set; }

        [DisplayName("最終承認者")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int top_approver_cd { get; set; }

        public ApproverModel? approver1 { get; set; }
        public ApproverModel? approver2 { get; set; }
        public ApproverModel top_approver { get; set; } = new();

        public List<WorkFlowViewModelStaff>? staffList = new();
        public List<WorkFlowViewModelStaff>? top_staffList = new();

        public List<WorkFlowViewModelRequestType>? requestTypeList = new();
    }

    public class ApproverModel
    {
        public string? approver_name { get; set; }

        public string? approve_date { get; set; }

        public string? comment { get; set; }

        public byte approve_result { get; set; } // 1: 承認 | 2: 否決 | 未処理
    }

    public class ApproveDetailViewModel
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
        public WorkFlowFileModel fileModel { get; set; } = new();

        [DisplayName("申請区分")]
        public int request_type { get; set; }

        public string? request_type_name { get; set; }

        [DisplayName("申請者")]
        public int requester_cd { get; set; }

        public string? requester_name { get; set; } // staff_name

        [DisplayName("承認者コメント")]
        [RequiredIfReject(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string? comment { get; set; }

        public byte is_accept { get; set; } // 0: reject | 1: accept

        public int is_top_approver { get; set; }
    }
}