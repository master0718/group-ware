$(function () {
    $(".back").on('click', function () {
        var path_dir_delete = $('#work_dir').val()
        var dic_cd = $("#dic_cd").val()
        $.ajax({
            type: "GET",
            url: `${baseUrl}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        })

        var id = $('#id').val();
        window.location.href = baseUrl + `Todo/Update?id=${id}`
    })

    $('.btnAddComment').on('click', function (e) {
        $('#commentForm').trigger("submit");
    })
})

$('.Report_save_comment_no').on('click', function () {
    $('#already_read_comment_no').val($(this).attr('data-comment_no'));
});