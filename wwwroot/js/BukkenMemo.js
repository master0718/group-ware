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
//let target = document.getElementById('scroll-inner');
//target.scrollIntoView(false);

$(function () {
//    var top = sessionStorage.getItem("topposition");
//    //window.scroll({
//    //    top: OffsetTop,
//    //    behavior: 'smooth',

//    //});
//    window.scroll(0, top);
//    // sessionStorage削除
//    sessionStorage.removeItem("topposition");
});

$('.check_comment').on('click', function () {
    $('#already_read_comment_no').val($(this).attr('data-comment_no'));
    var bukn_cd = $('#bukn_cd').val();
    var comment_no = $(this).attr('data-comment_no');
    var a = $(this);
    var checked_count = $('#_Checked_count_' + $(this).attr('data-data_no'));
    var checked_member = $('#_Checked_member_' + $(this).attr('data-data_no'));
    $.ajax({
        type: "Get",
        url: baseUrl + 'BukkenMemo/Check_comment?bukn_cd=' + bukn_cd + '&already_read_comment_no=' + comment_no,
        success: function (ret, status, xhr) {
            if (ret != null) {
                a.text(ret[0]);
                checked_count.text(ret[1]);
                checked_member.empty();
                for (var i = 0; i < ret[2].length; i++) {
                    checked_member.append('<div>' + ret[2][i] + '</div>');
                }
                console.log("成功");
            } else {
                console.log("失敗");
            }
        },
        error: function (e) {
            //レスポンスが返って来ない場合
            console.log("失敗：　" + e);
        },
    });

});

