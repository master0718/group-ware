$('.check_comment').on('click', function () {
    var report_no = $('#report_no').val();
    var comment_no = $(this).attr('data-comment_no');
    var a = $(this);
    var checked_count = $('#_Checked_count_' + $(this).attr('data-data_no'));
    var checked_member = $('#_Checked_member_' + $(this).attr('data-data_no'));
    $.ajax({
        type: "Get",
        url: baseUrl + 'Report/Check_comment?report_no=' + report_no + '&comment_no=' + comment_no,
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
                alert("失敗");
                console.log("失敗");
            }
        },
        error: function (e) {
            //レスポンスが返って来ない場合
            alert("失敗");
            console.log("失敗：　" + e);
        },
    });

});
$('.check_main').on('click', function () {
    var report_no = $('#report_no').val();
    var a = $(this);
    var checked_count = $('#_Checked_count' );
    var checked_member = $('#_Checked_member');
    $.ajax({
        type: "Get",
        url: baseUrl + 'Report/Check_comment_main?report_no=' + report_no,
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
                alert("失敗");
                console.log("失敗");
            }
        },
        error: function (e) {
            //レスポンスが返って来ない場合
            alert("失敗");
            console.log("失敗：　" + e);
        },
    });

});

