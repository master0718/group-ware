$(".check_main").on("click", function () {
  var a = $(this);
  var checked_count = $("#_Checked_count");
  var checked_member = $("#_Checked_member");
  $.ajax({
    type: "Get",
    url: baseUrl + "Memo/Check_comment_main?memo_no=" + memo_no,
    success: function (ret, status, xhr) {
      if (ret != null) {
        a.text(ret[0]);
        checked_count.text(ret[1]);
        checked_member.empty();
        for (var i = 0; i < ret[2].length; i++) {
          checked_member.append("<div>" + ret[2][i] + "</div>");
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
