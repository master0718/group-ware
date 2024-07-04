let visibleCommentCount = 0
const commentCountPerPage = 5

$(function () {

    visibleCommentCount = $(".comment-item").length

    $(".back").on('click', function () {
        var path_dir_delete = $('#work_dir').val()
        var dic_cd = $("#dic_cd").val()
        $.ajax({
            type: "GET",
            url: `${baseUrl}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        })

        window.location = baseUrl + "Board/Index"
    })

    $('.btnSave').on('click', function (e) {
        $('.loading').show()

        var fileNosRemove = fileRemoveList.join(',')

        $('#file_nos_remove').attr('value', fileNosRemove)

        $('#boardForm').trigger("submit")
    })

    $('.btnCreate').on('click', function (e) {
        $('.loading').show()

        $('#boardForm').trigger("submit")
    })

    $('#boardForm').on('submit', function (e) {

        var applicant_cd = $('#applicant').val()
        $('#applicant_cd').val(applicant_cd)
        var category = $('#board_category').val()
        $('#category_cd').val(category)

        if (!$(this).valid()) {
            $('.loading').hide()
        }
    })

    $('#btnAddComment').on('click', function (e) {
        var url = baseUrl + `Board/AddComment`
        let board_no = $("#board_no").val()
        let message = $("#message").val()
        $.ajax({
            method: "POST",
            url: url,
            data: {
                board_no: board_no,
                message: message
            },
            success: function (result) {
                console.log(result)
                if (result.comment_no !== undefined) {
                    updateOnCommentAdded(result.comment_no, result.update_date, message)
                }
            },
            error: function (xhr) {
                alert("Error:" + xhr.responseText)
            }
        })
    })

    $("#btnMore").on('click', function () {
        let board_no = $("#board_no").val()
        let lastComment = $(".comment-item:last-child").attr("id")
        let lastCommentId = lastComment.substr(2)
        var url = baseUrl + `Board/GetMoreCommentList?board_no=${board_no}&last_comment_no=${lastCommentId}`
        $.ajax({
            method: "GET",
            url: url,
            success: function (result) {
                console.log(result)
                if (result.commentList !== undefined)
                    updateOnMore(result.commentList)
            },
            error: function (xhr) {
                alert("Error:" + xhr.responseText)
            }
        })
    })

    function updateOnMore(commentList) {
        visibleCommentCount += commentList.length

        var html = ''
        for (var i = 0; i < commentList.length; i++) {
            var item = commentList[i]
            html += `<div class="comment-item d-flex" id="C-${item.comment_no}">
                        <span class="avatar-title rounded-circle bg-success text-white">${item.registrant_name[0]}</span>
                        <div class="flex-1 pt-1 ps-2">
                            <div class="fw-bold pb-2">
                                ${item.registrant_name}
                                <small class="text-muted fw-normal float-end pt-1 register-date">${item.register_date}</small>
                            </div>
                            <span class="comment-message">${item.message}</span>
                        </div>
                    </div>`
        }
        $("#comment-list").append($(html))

        totalCommentCount = Number($("#comment-count").html())
        if (visibleCommentCount >= totalCommentCount) {
            $("#btnMore").parent().addClass("d-none")
        }
    }

    function updateOnCommentAdded(comment_no, update_date, message) {
        $("#message").val("")
        
        var visibleCommentCount_ = $(".comment-item").length
        var visibleCommentCountMax = visibleCommentCount == 0 ? commentCountPerPage : (visibleCommentCount % 5 == 0 ? visibleCommentCount : (visibleCommentCount - visibleCommentCount % commentCountPerPage + commentCountPerPage))
        console.log(visibleCommentCount_)
        console.log(visibleCommentCountMax)

        visibleCommentCount = visibleCommentCount_ + 1
        if (visibleCommentCount_ + 1 > visibleCommentCountMax) {
            $("#btnMore").parent().removeClass("d-none")
            $(".comment-item:last-child").remove()
            visibleCommentCount = visibleCommentCountMax
        }

        var update_user = $("#registrant_name").val()
        var html = `<div class="comment-item d-flex" id="C-${comment_no}">
                        <span class="avatar-title rounded-circle bg-success text-white">${update_user[0]}</span>
                        <div class="flex-1 pt-1 ps-2">
                            <div class="fw-bold pb-2">
                                ${update_user}
                                <small class="text-muted fw-normal float-end pt-1 register-date">${update_date}</small>
                            </div>
                            <span class="comment-message">${message}</span>
                        </div>
                    </div>`
        $("#comment-list").append($(html))

        var commentCount = Number($("#comment-count").html()) + 1
        $("#comment-count").html(commentCount)
    }
})
