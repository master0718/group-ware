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

    $('.btnEdit').on('click', function () {
        let itemId = $("#board_no").val();
        window.location.href = baseUrl + `Board/Update?id=${itemId}`
    })
    if (!isEditable) {
        $(".delete_file").addClass('d-none');
    }
    $('.btnSave').on('click', function (e) {
        action = "submit";
        $('.loading').show()

        var fileNosRemove = fileRemoveList.join(',')

        $('#file_nos_remove').attr('value', fileNosRemove)

        $('#boardForm').trigger("submit")
    })

    $('.btnCreate').on('click', function (e) {
        action = "submit";
        $('.loading').show()

        $('#boardForm').trigger("submit")
    })

    $('#btnShowOnTop').on('click', function (e) {
        let board_no = $('#board_no').val()
        var url = baseUrl + `Board/UpdateTop?board_no=${board_no}`
        var currVal = $(this).data('val')
        
        $.ajax({
            method: "GET",
            url: url,
            success: function (result) {
                if (currVal == true) {
                    $('#btnShowOnTop').text("トップに出す")
                    $('#btnShowOnTop').data('val', false)
                } else {
                    $('#btnShowOnTop').text("トップから消す")
                    $('#btnShowOnTop').data('val', true)
                }
            },
            error: function (xhr) {
                alert("Error:" + xhr.responseText)
            }
        })
    })

    $('#boardForm').on('submit', function (e) {
        var applicant_cd = $('#applicant').val()
        $('#applicant_cd').val(applicant_cd)
        var category = $('#board_category').val()
        $('#category_cd').val(category)
        var show_on_top = $('#btnShowOnTop').data('val')
        $('#show_on_top').val(show_on_top)

        if (!$(this).valid()) {
            $('.loading').hide()
        }
    })

    

    $('#comment_drag_area input[type="file"]').on('change', function () {
        checkButtonState();
    });

    // Event listener for textarea input
    $('#message').on('input', function () {
        checkButtonState();
    });

    // Initial check
    checkButtonState();

    $('#btnAddComment').on('click', function (e) {
        var url = baseUrl + `Board/AddComment`
        let board_no = $("#board_no").val()
        let message = $("#message").val()
        let work_dir = $("#comment_work_dir").val()
        $(this).prop('disabled', true);
        $.ajax({
            method: "POST",
            url: url,
            data: {
                board_no: board_no,
                message: message,
                work_dir: work_dir
            },
            success: function (result) {
                console.log(result)
                if (result.comment_no !== undefined) {
                    $("#comment_work_dir").val(result.comment_work_dir);
                    updateOnCommentAdded(result.comment_no, result.update_date, message, result.files)
                }
            },
            error: function (xhr) {
                alert("Error:" + xhr.responseText)
            }
        })
    })

    $('.check_main').on('click', function () {
        var board_no = $('#board_no').val()
        var a = $(this);
        var checked_count = $('#_Checked_count' )
        var checked_member = $('#_Checked_member')
        $.ajax({
            type: "Get",
            url: baseUrl + 'Board/Check_comment_main?board_no=' + board_no,
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

    function checkButtonState() {
        var filesSelected = $('#comment_drag_area input[type="file"]').get(0).files.length > 0;
        var messageNotEmpty = $('#message').val().trim().length > 0;

        if (filesSelected || messageNotEmpty) {
            $('#btnAddComment').prop('disabled', false);
        } else {
            $('#btnAddComment').prop('disabled', true);
        }
    }
    
    function updateOnMore(commentList) {
        visibleCommentCount += commentList.length

        var html = ''
        let board_no = $("#board_no").val()
        
        for (var i = 0; i < commentList.length; i++) {
            var item = commentList[i]
            var fileHtml = ''
            if (item.commentFileDetailList && item.commentFileDetailList.length > 0) {
                fileHtml += '<div class="row"><div class="col d-flex">';
                item.commentFileDetailList.forEach(file => {
                    var extension = file.filename.split('.').pop().toLowerCase();

                    var icon = file.filename.split('.').pop() + '.svg';
                    fileHtml += `
                        <div class="div_icon_child dropdown fileAreaHeitWidth">
                            <input type="hidden" value="${file.filename}">
                            <input type="hidden" value="${file.file_no}">
                            <button class="border-0 p-0 dropdown-toggle btn_file fileAreaInnerWidth" type="button" data-bs-toggle="dropdown" aria-expanded="false" style="background-color: var(--bs-card-bg);">
                                <div class="div_tooltip" data-toggle="tooltip" data-placement="top" title="${file.filename}">
                                    <div class="div_img_file bg-light p-2">
                                        <img src="${baseUrl}/images/file-icons/${icon}" alt="icon" style="height: 50px;">
                                    </div>
                                    <div class="text-wrap">${file.filename}</div>
                                </div>
                            </button>
                            <ul class="dropdown-menu fileAreaInnerWidth text-center">
                                <button class="dropdown-item comment_download_file" type="button" role="button" data-dir_kind="1" data-file_name="${file.filename}" data-comment_no="${file.comment_no}">ﾀﾞｳﾝﾛｰﾄﾞ</button>`;
                    if (PREVIEW_ALLOWED_EXTENSION_LIST.includes(`.${extension}`)) {
                        const dir_no = `${board_no}\\${file.comment_no}`;
                        const link = document.createElement('a');
                        link.className = 'dropdown-item preview_file site_iframe_preview';
                        link.href = `${baseUrl}Base/PreviewFile?dic_cd=4&dir_no=${dir_no}&file_name=${file.filename}`;
                        link.innerText = 'ﾌﾟﾚﾋﾞｭｰ';
                        
                        fileHtml += link.outerHTML;
                    }
                    fileHtml += `
                            </ul>
                        </div>
                    `;
                });
                fileHtml += '</div></div>';
            }

            html += `<div class="comment-item d-flex" id="C-${item.comment_no}">
                        <span class="avatar-title rounded-circle bg-success text-white">${item.registrant_name[0]}</span>
                        <div class="flex-1 pt-1 ps-2">
                            <div class="fw-bold pb-2">
                                ${item.registrant_name}
                                <small class="text-muted fw-normal float-end pt-1 register-date">${item.register_date}</small>
                            </div>
                            <span class="comment-message">${item.message}</span>
                            ${fileHtml}
                        </div>
                    </div>`
        }
        $("#comment-list").append($(html))
        $('.site_iframe_preview').colorbox(
            {
                iframe: true,
                width: "90%", height: "90%",
                opacity: 0.5,
                scrolling:false
            });
        totalCommentCount = Number($("#comment-count").html())
        if (visibleCommentCount >= totalCommentCount) {
            $("#btnMore").parent().addClass("d-none")
        }
    }

    function updateOnCommentAdded(comment_no, update_date, message, files) {
        $("#message").val("")
        $("input[type='file'][asp-for='@Model.CommentFile']").val('')
        $("#comment_div_icon").empty()
        
        var visibleCommentCount_ = $(".comment-item").length
        var visibleCommentCountMax = visibleCommentCount == 0 ? commentCountPerPage : (visibleCommentCount % 5 == 0 ? visibleCommentCount : (visibleCommentCount - visibleCommentCount % commentCountPerPage + commentCountPerPage))
        console.log(visibleCommentCount_)
        console.log(visibleCommentCountMax)
        let board_no = $("#board_no").val()

        visibleCommentCount = visibleCommentCount_ + 1
        if (visibleCommentCount_ + 1 > visibleCommentCountMax) {
            $("#btnMore").parent().removeClass("d-none")
            $(".comment-item:last-child").remove()
            visibleCommentCount = visibleCommentCountMax
        }
        var fileHtml = '';
        if (files && files.length > 0) {
            fileHtml += '<div class="row"><div class="col d-flex">';
            files.forEach(file => {
                var icon = file.filename.split('.').pop() + '.svg';
                var extension = file.filename.split('.').pop().toLowerCase();

                fileHtml += `
                    <div class="div_icon_child dropdown fileAreaHeitWidth">
                        <input type="hidden" value="${file.filename}">
                        <input type="hidden" value="${file.file_no}">
                        <button class="border-0 p-0 dropdown-toggle btn_file fileAreaInnerWidth" type="button" data-bs-toggle="dropdown" aria-expanded="false" style="background-color: var(--bs-card-bg);">
                            <div class="div_tooltip" data-toggle="tooltip" data-placement="top" title="${file.filename}">
                                <div class="div_img_file bg-light p-2">
                                    <img src="${baseUrl}/images/file-icons/${icon}" alt="icon" style="height: 50px;">
                                </div>
                                <div class="text-wrap">${file.filename}</div>
                            </div>
                        </button>
                        <ul class="dropdown-menu fileAreaInnerWidth text-center">
                            <button class="dropdown-item comment_download_file" type="button" role="button" data-dir_kind="1" data-file_name="${file.filename}" data-comment_no="${file.comment_no}">ﾀﾞｳﾝﾛｰﾄﾞ</button>`;
                        if (PREVIEW_ALLOWED_EXTENSION_LIST.includes(`.${extension}`)) {
                            const dir_no = `${board_no}\\${file.comment_no}`;
                            const link = document.createElement('a');
                            link.className = 'dropdown-item preview_file site_iframe_preview';
                            link.href = `${baseUrl}Base/PreviewFile?dic_cd=4&dir_no=${dir_no}&file_name=${file.filename}`;
                            link.innerText = 'ﾌﾟﾚﾋﾞｭｰ';
                            
                            fileHtml += link.outerHTML;
                        }
                fileHtml += `</ul>
                    </div>
                `;
            });
            fileHtml += '</div></div>';
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
                            ${fileHtml}
                        </div>
                    </div>`
        $("#comment-list").append($(html))
        $('.site_iframe_preview').colorbox(
            {
                iframe: true,
                width: "90%", height: "90%",
                opacity: 0.5,
                scrolling:false
            });
        var commentCount = Number($("#comment-count").html()) + 1
        $("#comment-count").html(commentCount)
    }
})
