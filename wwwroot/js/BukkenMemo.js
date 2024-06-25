//// 確認ボタンをクリックするとイベント発動
//$('.kakuninDialog').click(function () {
//    //$("#frm").validate()
//    if ($("#frm").valid()) {
//        // もしキャンセルをクリックしたら
//        if (!confirm('登録してもよろしいですか？')) {

//            // submitボタンの効果をキャンセルし、クリックしても何も起きない
//            return false;

//            // 「OK」をクリックした際の処理を記述
//        } else {
//            $("#frm").trigger("submit");

//        }
//    }
//});
let target = document.getElementById('scroll-inner');
target.scrollIntoView(false);

$(function () {
        $(document).find('.BaseDialogStaff_js_area_options').addClass('bg-white');
});
$('.Report_save_comment_no').on('click', function () {
    $('#already_read_comment_no').val($(this).attr('data-comment_no'));
});

