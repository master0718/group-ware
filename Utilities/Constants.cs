namespace web_groupware.Utilities
{
    public class ClaimTypes
    {
        public const string STAF_CD = "staf_cd";
    }
    public class DataTypes
    {
        public const int SCHEDULE_NO = 1;
        public const int BUKKEN_COMMENT_NO = 2;
        public const int REPORT_NO = 4;
        public const int REPORT_COMMENT_NO = 5;
        public const int INFO_CD = 6;
        //public const int FILEINFO_NO = 8;
        public const int WORKFLOW_NO = 9;
        public const int MEMO_NO = 10;
        public const int INFO_FILE_NO = 11;
        //public const int MEMO_READ_NO = 12;
        //public const int SCHEDULE_FACILITY_NO = 13;
        public const int BOARD_NO = 14;
        public const int BOARD_COMMENT_NO = 15;
        public const int FILE_NO = 16;
        public const int BUKKENCOMMENT_FILE_NO = 17;
        public const int TODO_NO = 18;
        public const int INFO_PERSONAL_NO = 19;
        public const int CHECK_NO = 20;
        public const int BOARDCOMMENT_FILE_NO = 21;
    }
    public class DIC_KB
    {
        public const int URL_PROJECT = 1;
        public const int SAVE_PATH_FILE = 700;
        public const int TEMPLETE_FILE = 701;
        public const int MEMO_STATUS = 710;
        public const int ATTENDANCE_STATUS = 711;
        public const int WORKFLOW_REQ_TYPE = 712;
        public const int WORKFLOW_APPROVE_STATUS = 713;
        public const int BOARD_CATEGORY = 720;
    }

    public class DIC_KB_700_DIRECTORY
    {
        public const string NOTICE = "1";
        public const string REPORT = "2";
        public const string FILEINFO = "3";
        public const string BOARD = "4";
        public const string SCHEDULE = "5";
        public const string BUKKENCOMMENT_FILE = "6";
        public const string TODO = "8";
        public const string WORKFLOW = "9";
    }
    public class DIC_KB_701_TEMPLETE_FILE
    {
        public const string ATTENDANCE = "1";
    }

    public static class MemoTypes
    {
        public const string All = "すべて";
        public const string Unread = "未読";
        public const string Read = "既読";
        public const string Working = "対応中";
        public const string Finish = "済";
        public const string Checked = "確認済";
        public static string[] AllTypes = { All, Unread, Read, Checked };
    }

    public static class MemoCategory
    {
        public const int ALL = 0;
        public const int SENT = 1;
        public const int RECEIVED = 2;
    }

    public class BoardStatus
    {
        public const int UPCOMING = 0; // 未対応
        public const int REQUESTING = 1; // 依頼中
        public const int IN_PROGRESS = 2; // 対応中
        public const int COMPLETED = 3; // 完了
        public static string[] All = { "未対応", "依頼中", "対応中", "完了" };
    }

    public class WorkflowApproveStatus
    {
        public const string All = "すべて";
        public const string DRAFT_NAME = "作成中";
        public const string REQUEST_NAME = "申請中";
        public const string APPROVE_NAME = "承認中";
        public const string TOP_APPROVE_NAME = "最終承認中";
        public const string REJECT_NAME = "否　決";
        public const string FINISH_NAME = "完　了";

        public const byte NONE = 0;
        public const byte DRAFT = 1;
        public const byte REQUEST = 2;
        public const byte APPROVE = 3;
        public const byte TOP_APPROVE = 4;
        public const byte REJECT = 5;
        public const byte FINISH = 6;

        public static string[] AllStatus = { All, DRAFT_NAME, REQUEST_NAME, APPROVE_NAME, TOP_APPROVE_NAME, REJECT_NAME, FINISH_NAME };
    }

    public static class WorkflowApproveResult
    {
        public const byte NONE = 0;
        public const byte ACCEPT = 1;
        public const byte REJECT = 2;
    }

    public class SCHEDULE_REPETITION
    {
        public const int NONE = 0;
        public const int DAILY = 1;
        public const int DAILY_NO_HOLIDAY = 2;
        public const int WEEKLY = 3;
        public const int MONTHLY = 4;
    }

    public class INFO_PERSONAL_PARENT_ID
    {
        public const int T_REPORT = 1;
        public const int T_REPORTCOMMENT = 2;
        public const int T_MEMO = 3;
        public const int T_BUKKENCOMMENT = 4;
        public const int T_BOARD = 5;
    }

    public class CHECK_PARENT_ID
    {
        public const int T_REPORT = 1;
        public const int T_REPORTCOMMENT = 2;
        public const int T_MEMO = 3;
        public const int T_BUKKENCOMMENT = 4;
        public const int T_BOARD = 5;
    }

    public class WORKFLOW_REQ_TYPE
    {
        public const int REASON1 = 1;
        public const int REASON2 = 2;
        public const int REASON3 = 3;
        public const int REASON4 = 4;
    }
    public class PREVIEW_ALLOWED_EXTENSION
    {
        public static readonly List<string> LIST = new List<string> { ".png", ".jpg", ".jpeg", ".heic", ".gif", ".pdf" };
    }
    public class UPLOAD_FILE_ALLOWED_EXTENSION
    {
        public const string IMAGE_PDF = "png,jpg,jpeg,heic,gif,pdf";
    }

}
