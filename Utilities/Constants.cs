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
        public const int FILEINFO_NO = 8;
        public const int WORKFLOW_NO = 9;
        public const int MEMO_NO = 10;
        public const int INFO_FILE_NO = 11;
        public const int MEMO_READ_NO = 12;
        public const int SCHEDULE_FACILITY_NO = 13;
        public const int BOARD_NO = 14;
        public const int BOARD_COMMENT_NO = 15;
        public const int FILE_NO = 16;
        public const int BUKKENCOMMENT_FILE_NO = 17;
        public const int TODO_NO = 18;
        public const int TODO_COMMENT_NO = 19;
    }
    public class DIC_KB
    {
        public const int URL_PROJECT = 1;
        public const int SAVE_PATH_FILE = 700;
        public const int TEMPLETE_FILE = 701;
        public const int MEMO_STATUS = 710;
        public const int ATTENDANCE_STATUS = 711;
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
        public const string TODO_COMMENT = "10";
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

        public static string[] AllTypes = { All, Unread, Read, Working, Finish };
    }

    public class BoardStatus
    {
        public const int UPCOMING = 0; // 未対応
        public const int REQUESTING = 1; // 依頼中
        public const int IN_PROGRESS = 2; // 対応中
        public const int COMPLETED = 3; // 完了
    }
    public class SCHEDULE_REPETITION
    {
        public const int NONE = 0;
        public const int DAILY = 1;
        public const int DAILY_NO_HOLIDAY = 2;
        public const int WEEKLY = 3;
        public const int MONTHLY = 4;
    }
}
