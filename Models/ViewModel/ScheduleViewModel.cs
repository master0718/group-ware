using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using web_groupware.Utilities;

namespace web_groupware.Models
{
    public class ScheduleViewModel
    {
        public List<StaffModel>? StaffList;
        public List<EmployeeGroupModel>? GroupList;
        public List<T_PLACEM> PlaceList { get; set; } = new List<T_PLACEM>();
        public string? startDate;
        public int staf_cd;
        public bool is_people;
        public string? staf_name;
    }

    public class ScheduleTypeModel
    {
        public int schedule_type;
        public string? schedule_typename;
        public string color;
    }
    public class ScheduleFileModel
    {
        public List<T_SCHEDULE_FILE> fileList { get; set; } = new List<T_SCHEDULE_FILE>();
        public int editable = 0;
    }

    public class ScheduleDetailViewModel
    {
        public int schedule_no { get; set; }

        [DisplayName("予定")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        public int schedule_type { get; set; }
        public List<ScheduleTypeModel> ScheduleTypeList { get; set; } = new List<ScheduleTypeModel>();

        [DisplayName("開始時間")]
        public string? start_datetime { get; set; }

        [DisplayName("終了時間")]
        public string? end_datetime { get; set; }

        [DisplayName("タイトル")]
        [Required(ErrorMessage = Messages.REQUIRED)]
        [MaxLength(1000, ErrorMessage = Messages.MAXLENGTH)]
        public string title { get; set; } = string.Empty;

        [DisplayName("メモ")]
        [MaxLength(2000, ErrorMessage = Messages.MAXLENGTH)]
        public string? memo { get; set; } = string.Empty;

        [DisplayName("添付ファイル")]
        public List<IFormFile> File { get; set; } = new List<IFormFile>();
        public ScheduleFileModel fileModel { get; set; } = new ScheduleFileModel();
        public string? work_dir { get; set; }

        public string? Delete_files { get; set; }
        public string? file_nos_remove { get; set; }

        [DisplayName("参加者")]
        //[Required(ErrorMessage = Messages.REQUIRED)]
        [MinLength(1, ErrorMessage = Messages.REQUIRED)]
        public string[] MyStaffList { get; set; } = Array.Empty<string>(); // staff or group (S-1, G-1)

        [DisplayName("施設")]
        //[Required(ErrorMessage = Messages.REQUIRED)]
        [MinLength(1, ErrorMessage = Messages.REQUIRED)]
        public int[] MyPlaceList { get; set; } = Array.Empty<int>();

        [DisplayName("全スタッフ")]
        public List<StaffModel> StaffList { get; set; } = new List<StaffModel>();
        public List<EmployeeGroupModel> GroupList { get; set; } = new List<EmployeeGroupModel>();

        [DisplayName("全施設")]
        public List<PlaceModel> PlaceList { get; set; } = new List<PlaceModel>();

        [DisplayName("非公開")]
        public bool is_private { get; set; }

        // repetition
        public byte repeat_type { get; set; }

        public byte? every_on { get; set; }

        [DisplayName("開始時間")]
        public string? time_from { get; set; }

        [DisplayName("終了時間")]
        public string? time_to { get; set; }

        public string? repeat_date_from { get; set; }

        public string? repeat_date_to { get; set; }

        [DisplayName("登録者")]
        public string? create_user { get; set; }

        [DisplayName("登録日時")]
        public string? create_date { get; set; }

        [DisplayName("更新者")]
        public string? update_user { get; set; }

        [DisplayName("更新日時")]
        public string? update_date { get; set; }
    }
}
