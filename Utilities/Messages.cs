namespace web_groupware.Utilities
{
    public static class Messages
    {
        public const string ERROR_PREFIX = "アスター　エラー　";

        public const string MAXLENGTH = "{0}は文字以内で入力してください。";
        public const string REQUIRED = "{0}は必須項目です。";
        public const string LOGIN_ERROR_MESSAGE01 = "メールアドレス、またはパスワードが違います。";
        public const string IS_VALID_FALSE = "入力に誤りがあります。";
        public const string MAXLENGTH_FILE = "{0}は文字以内までです。他のファイルを選択してください。";
        public const string POSTNUMBER = "{0}は000-0000の形式で入力してください。";
        public const string KATAKANA = "{0}は全角カタカナ・全角スペースのみを入力してください。";
        public const string PHONE = "{0}は10桁以上の数字を入力してください。";
        public const string EMAILADDRESS = "正しいメールアドレスを入力してください。";
        public const string POSTCODE = "{0}は0600032または060-0032の形式で入力してください。";
        public const string DATE_FROM_TO = "{0}は1900/01/01から2099/12/31の日付を入力してください。";
        public const string BEFORE_NOW = "{0}は明日以降のの日付は入力できません。";
        public const string RANGE = "{0}は{1}～{2}の数字を入力してください。";
        public const string ZeroOrMore = "{0}は0以上の数字を入力してください。";
        public const string PASSWORD = "{0}は8文字以上30文字以内の半角英数字を入力して下さい。";
        public const string INITIAL_PASSWORD = "{0}は初期パスワードと同じパスワードは設定できません。";
        public const string SAME_PASSWORD = "新しいパスワードと新しいパスワード（確認）は同一の文字を入力してください。";
        public const string FILE_EXTENSIONS = "アップロード可能なファイルの種類はjpg、jpeg、png、heic、pdfのみです。";
        public const string FILE_SIZE_1 = "{0}はアップロードできません。";
        public const string FILE_SIZE_2 = "ファイルの最大サイズは5MBです。";
        public const string FOLDER_DUPLICATE = "同じ名前のフォルダがすでに存在しています。";
        public const string UPLOAD_FILE_DUPLICATE = "同じ名前のファイルをアップロードできません。";
        public const string MAX_FILE_COUNT_5 = "アップロードできるファイルは5ファイルまでです。";
        public const string MAX_FILE_SIZE_20MB = "ファイルの最大サイズは20MBです。";
        public const string MAX_FOLDER_NAME_LENGTH = "フォルダ名の長さは最大64文字です。";
        public const string DICTIONARY_FILE_PATH_NO_EXIST = "辞書に{0}, {1}のレコードが登録されていません。";
        public const string BOARD_ALLOWED_FILE_EXTENSIONS = "アップロード可能なファイルの種類はpdfのみです。";
        public const string REQUEST_WORKFLOW_EDIT_VIOLATION = "作成中の申請のみ変更できます。";
        public const string WORKFLOW_APPROVAL_ALREADY_FINISHED = "すでに完了した掲示物です。";
        public const string WORKFLOW_APPROVAL_ALREADY_REJECTED = "すでに否決された申請です。";
        public const string WORKFLOW_APPROVAL_NOT_APPROVAL = "申請や承認状態の掲示物ではありません。";
        public const string WORKFLOW_APPROVAL_APPROVER2_REQUIRED = "承認者2を選択してください。";
        public const string PLACE_ALREADY_RESERVED = "施設予約が重複されています。";
        public const string TODO_DEADLINE_REQUIRED = "期日を入力してください。";
        public const string FACILITY_REQUIRED = "施設を選択してください。";

        public const string ERROR_MESSAGE_001 = "ログイン時に利用しているメールアドレスを指定して下さい。";
        public const string SCOPE_MAKING_KNOWN = "周知範囲を選択して下さい。";
        

    }
    public class Message_register
    {
        public const string SUCCESS_001 = "登録が完了しました。";
        public const string FAILURE_001 = "登録に失敗しました。";



    }
    public class Message_change
    {
        public const string SUCCESS_001 = "変更が完了しました。";
        public const string FAILURE_001 = "変更に失敗しました。";



    }
    public class Message_delete
    {
        public const string SUCCESS_001 = "削除が完了しました。";
        public const string FAILURE_001 = "削除に失敗しました。";



    }

    public class Message_Email
    {
        public const string SUCCESS_001 = "メールが正常に送信されました。";
        public const string FAILURE_001 = "メール送信に失敗しました。";



    }
    public class Format
    {
        public const string ZeroUme_8 = "00000000";
        public const int FILE_SIZE = 5242880;



    }
    public class Board_comment
    {
        public const string CREATE = "新規登録されました。";
        public const string UPDATE = "更新されました。";
    }
    public class Check_button_text
    {
        public const string CHECK = "確認しました";
        public const string CANCEL = "「確認しました」を取り消す";
    }

}
