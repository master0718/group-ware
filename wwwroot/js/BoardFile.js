//画面遷移orクローズでワークフォルダ削除
let alreadyDeleted = false;
function deleteWorkDir() {
    if (!alreadyDeleted) {
        alreadyDeleted = true;

        let url = baseUrl;
        var path_dir_delete = $('#work_dir').val();
        var dic_cd = $("#dic_cd").val();
        document.cookie ="test="+ url + "Base/DeleteDirectory?work_dir=" + path_dir_delete + "&dic_cd=" + dic_cd;
    
        $.ajax({
            type: "GET",
            url: url + "Base/DeleteDirectory?work_dir=" + path_dir_delete + "&dic_cd=" + dic_cd
    
        //    url: `${url}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        });        
    }
}

let alreadyCommentDeleted = false;
function deleteCommentWorkDir() {
    if (!alreadyCommentDeleted) {
        alreadyCommentDeleted = true;

        let url = baseUrl;
        var path_dir_delete = $('#comment_work_dir').val();
        var dic_cd = $("#comment_dic_cd").val();
        document.cookie ="test="+ url + "Base/DeleteDirectory?work_dir=" + path_dir_delete + "&dic_cd=" + dic_cd;
    
        $.ajax({
            type: "GET",
            url: url + "Base/DeleteDirectory?work_dir=" + path_dir_delete + "&dic_cd=" + dic_cd
    
        //    url: `${url}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        });        
    }
}

window.addEventListener("pagehide", (event) => {
    if (action == null)
    {
        deleteWorkDir();
        deleteCommentWorkDir();
    }
});

window.onbeforeunload = function (e) {
    if (action == null)
    {
        deleteWorkDir();
        deleteCommentWorkDir();
    }
}

var isEditable = true
const _dataTransfer = new DataTransfer()
/**
 * @param {any} obj ファイルリスト
 */
function previewImage(obj) {
    var upload_file_allowed_extension_1 = $('#Upload_file_allowed_extension_1').val();//各機能のViewModelでBaseViewModelを継承し、Upload_file_allowed_extension_1にConstantsの値を設定
    var arr_upload_file_allowed_extension_1 = upload_file_allowed_extension_1.split(",");

    for (var i = 0; i < obj.files.length; i++) {
        if (!arr_upload_file_allowed_extension_1.includes(obj.files[i].type.split("/").pop())) {
            $("#fileArea .validation-extension-error").removeClass('d-none');
            setTimeout(function () {
                $("#fileArea .validation-extension-error").addClass('d-none');
            }, 2000);
            return;
        }
    }    
    var arr_fileName = [];
    //アップロード済みファイル名を追加
    console.log("保存済↓")
    document.getElementsByClassName('main_files').forEach(main_file => {
        arr_fileName.push($(main_file).text());
        console.log($(main_file).text());
    });
    //前回選択・ドロップファイル名を追加
    console.log("前回選択・ドロップ↓")
    _dataTransfer.files.forEach(file => {
        console.log(file.name);
        if (!arr_fileName.includes(file.name)) {
            arr_fileName.push(file.name)
        } else {
            var count = 1
            while (true) {
                var kandidat = file.name + '（' + count + '）';
                if (!arr_fileName.includes(kandidat)) {
                    file.name = kandidat;
                    arr_fileName.push(file.name);
                    console.log(file.name + "　ファイル名変更後");
                    break;
                }
                count++;
            }
        }
    });
    //今回選択・ドロップファイル名を検査・ファイル作り替え
    console.log("今回選択・ドロップ↓")
    const _dataTransfer_work = new DataTransfer()
    for (var i = 0; i < obj.files.length; i++) {
        console.log(obj.files[i]);
        if (!arr_fileName.includes(obj.files[i].name)) {
            arr_fileName.push(obj.files[i].name)
            _dataTransfer_work.items.add(obj.files[i])
        } else {
            var count = 1;
            while (true) {
                var arr_work = obj.files[i].name.split('.');
                var kandidat = "";
                for (var w = 0; w < arr_work.length - 1; w++) {
                    kandidat = kandidat + arr_work[w] + ".";
                }
                kandidat = kandidat.slice(0, -1);
                kandidat = kandidat + '（' + count + '）';
                // ファイルの拡張子を取得
                const fileExtention = obj.files[i].name.substring(obj.files[i].name.lastIndexOf(".") + 1);
                kandidat = kandidat + "." + fileExtention;

                if (!arr_fileName.includes(kandidat)) {
                    const blob = obj.files[i].slice(0, obj.files[i].size, obj.files[i].type);
                    // ファイル名称変更後のファイルオブジェクト
                    const renamedFile = new File([blob], kandidat, { type: obj.files[i].type });

                    //obj.files[i] = renamedFile;
                    arr_fileName.push(kandidat);
                    _dataTransfer_work.items.add(renamedFile);
                    console.log(renamedFile.name + "　ファイル名変更後");
                    break;
                }
                count++;
            }
        }
    }
    //今回選択・ドロップファイル　HTML追加
    console.log("HTML追加");
    _dataTransfer_work.files.forEach(file => {
        _dataTransfer.items.add(file)

        add_div_icon(file.name, true);
    });

    $('.div_tooltip').tooltip();
    //file置き換え
    console.log("file置き換え");
    document.getElementById('File').files = _dataTransfer.files;

    //今回選択・ドロップファイル　ファイルアップロード追加
    console.log("ファイルアップロード↓");
    _dataTransfer_work.files.forEach(file => {
        console.log("アップロード開始　" + file.name);
        let url = baseUrl;
        var formData = new FormData();
        formData.append("file", file);
        formData.append("dic_cd", $("#dic_cd").val());
        formData.append("work_dir", $("#work_dir").val());
        var progressBar = document.getElementById('prog_' + file.name);
        //var progressValue = document.getElementById('pv');
        $.ajax({
            url: url + "Base/UploadFile",
            type: 'POST',
            processData: false,
            contentType: false,
            async: true,
            data: formData,
            xhr: function () {
                var XHR = new XMLHttpRequest();
                if (XHR.upload) {
                    XHR.upload.addEventListener('progress', function (e) {
                        var progVal = parseInt(e.loaded / e.total * 10000) / 100;
                        progressBar.value = progVal;
                        //progressValue.innerHTML = progVal + '%';
                    }, false);
                }
                return XHR;
            },
            success: function (data) {
                console.log(data);
                console.log("アップロード完了　" + file.name);
            },
            error: function () {
                console.log("アップロード失敗　" + file.name);
            }
        });
    });

}
const _commentDataTransfer = new DataTransfer()
/**
 * @param {any} obj ファイルリスト
 */
function previewCommentImage(obj) {

    var upload_file_allowed_extension_1 = $('#Upload_file_allowed_extension_1').val();//各機能のViewModelでBaseViewModelを継承し、Upload_file_allowed_extension_1にConstantsの値を設定
    var arr_upload_file_allowed_extension_1 = upload_file_allowed_extension_1.split(",");

    for (var i = 0; i < obj.files.length; i++) {
        if (!arr_upload_file_allowed_extension_1.includes(obj.files[i].type.split("/").pop())) {
            $("#commentFileArea .validation-extension-error").removeClass('d-none');
            setTimeout(function () {
                $("#commentFileArea .validation-extension-error").addClass('d-none');
            }, 2000);
            return;
        }
    }    
    var arr_fileName = [];
    //アップロード済みファイル名を追加
    console.log("保存済↓")
    document.getElementsByClassName('main_files').forEach(main_file => {
        arr_fileName.push($(main_file).text());
        console.log($(main_file).text());
    });
    //前回選択・ドロップファイル名を追加
    console.log("前回選択・ドロップ↓")
    _commentDataTransfer.files.forEach(file => {
        console.log(file.name);
        if (!arr_fileName.includes(file.name)) {
            arr_fileName.push(file.name)
        } else {
            var count = 1
            while (true) {
                var kandidat = file.name + '（' + count + '）';
                if (!arr_fileName.includes(kandidat)) {
                    file.name = kandidat;
                    arr_fileName.push(file.name);
                    console.log(file.name + "　ファイル名変更後");
                    break;
                }
                count++;
            }
        }
    });
    //今回選択・ドロップファイル名を検査・ファイル作り替え
    console.log("今回選択・ドロップ↓")
    const _commentDataTransfer_work = new DataTransfer()
    for (var i = 0; i < obj.files.length; i++) {
        console.log(obj.files[i]);
        if (!arr_fileName.includes(obj.files[i].name)) {
            arr_fileName.push(obj.files[i].name)
            _commentDataTransfer_work.items.add(obj.files[i])
        } else {
            var count = 1;
            while (true) {
                var arr_work = obj.files[i].name.split('.');
                var kandidat = "";
                for (var w = 0; w < arr_work.length - 1; w++) {
                    kandidat = kandidat + arr_work[w] + ".";
                }
                kandidat = kandidat.slice(0, -1);
                kandidat = kandidat + '（' + count + '）';
                // ファイルの拡張子を取得
                const fileExtention = obj.files[i].name.substring(obj.files[i].name.lastIndexOf(".") + 1);
                kandidat = kandidat + "." + fileExtention;

                if (!arr_fileName.includes(kandidat)) {
                    const blob = obj.files[i].slice(0, obj.files[i].size, obj.files[i].type);
                    // ファイル名称変更後のファイルオブジェクト
                    const renamedFile = new File([blob], kandidat, { type: obj.files[i].type });

                    //obj.files[i] = renamedFile;
                    arr_fileName.push(kandidat);
                    _commentDataTransfer_work.items.add(renamedFile);
                    console.log(renamedFile.name + "　ファイル名変更後");
                    break;
                }
                count++;
            }
        }
    }
    //今回選択・ドロップファイル　HTML追加
    console.log("HTML追加");
    _commentDataTransfer_work.files.forEach(file => {
        _commentDataTransfer.items.add(file)

        add_comment_div_icon(file.name, true);
    });

    $('.comment_div_tooltip').tooltip();
    //file置き換え
    console.log("file置き換え");
    document.getElementById('CommentFile').files = _commentDataTransfer.files;

    //今回選択・ドロップファイル　ファイルアップロード追加
    console.log("ファイルアップロード↓");
    _commentDataTransfer_work.files.forEach(file => {
        console.log("アップロード開始　" + file.name);
        let url = baseUrl;
        var formData = new FormData();
        formData.append("file", file);
        formData.append("dic_cd", $("#comment_dic_cd").val());
        formData.append("work_dir", $("#comment_work_dir").val());
        var progressBar = document.getElementById('prog_' + file.name);
        //var progressValue = document.getElementById('pv');
        $.ajax({
            url: url + "Base/UploadFile",
            type: 'POST',
            processData: false,
            contentType: false,
            async: true,
            data: formData,
            xhr: function () {
                var XHR = new XMLHttpRequest();
                if (XHR.upload) {
                    XHR.upload.addEventListener('progress', function (e) {
                        var progVal = parseInt(e.loaded / e.total * 10000) / 100;
                        progressBar.value = progVal;
                        //progressValue.innerHTML = progVal + '%';
                    }, false);
                }
                return XHR;
            },
            success: function (data) {
                console.log(data);
                console.log("アップロード完了　" + file.name);
            },
            error: function () {
                console.log("アップロード失敗　" + file.name);
            }
        });
    });

}

$(function () {
    //ツールチップと枠線
    var list_btn_file = $('.div_tooltip');
    for (var i = 0; i < list_btn_file.length; i++) {
        $(list_btn_file[i]).tooltip();
    }
    $('.btn_file').on('mouseenter', function (e) {
        $(this).find('.div_img_file').addClass('border');
        $(this).find('.div_img_file').addClass('border-info');
    });
    $('.btn_file').on('mouseleave', function (e) {
        $(this).find('.div_img_file').removeClass('border');
        $(this).find('.div_img_file').removeClass('border-info');
    });

    var comment_list_btn_file = $('.comment_div_tooltip');
    for (var i = 0; i < comment_list_btn_file.length; i++) {
        $(comment_list_btn_file[i]).tooltip();
    }
    $('.comment_btn_file').on('mouseenter', function (e) {
        $(this).find('.comment_div_img_file').addClass('border');
        $(this).find('.comment_div_img_file').addClass('border-info');
    });
    $('.comment_btn_file').on('mouseleave', function (e) {
        $(this).find('.comment_div_img_file').removeClass('border');
        $(this).find('.comment_div_img_file').removeClass('border-info');
    });
    // #region ファイル追加（クリップボード・選択ボタン・ドロップ）
    //クリップボードの貼り付け
    // document.addEventListener("paste", function (e) {
    //     if (isEditable !== undefined && isEditable === false) return
    //     const _commentDataTransfer_clip = new DataTransfer()
    //     // event からクリップボードのアイテムを取り出す
    //     var items = e.clipboardData.items; // ここがミソ
    //     for (var i = 0; i < items.length; i++) {
    //         var item = items[i];
    //         if (item.type.indexOf("image") != -1) {
    //             // 画像のみサーバへ送信する
    //             var image = item.getAsFile();
    //             _commentDataTransfer_clip.items.add(image);
    //         }
    //     }
    //     previewCommentImage(_commentDataTransfer_clip);
    // });
    //選択ボタンでファイルが選択さえｒた時
    $('#File').on('change', function (e) {
        previewImage(this);
    });
    $('#CommentFile').on('change', function (e) {
        previewCommentImage(this);
    });
    // ファイルドラッグアンドドロップ
    // ドラッグしている要素がドロップされたとき
    $('.dropArea').on('drop', function (event) {
        if (isEditable === false) return
        event.preventDefault();
        previewImage(event.originalEvent.dataTransfer);
    });

    $('.commentDropArea').on('drop', function (event) {
        event.preventDefault();
        previewCommentImage(event.originalEvent.dataTransfer);
    });
    // #endregion

    // #region ドロップ対象外のオブジェクト抑制(ファイルがダウンロードされるため)
    $(document).on('dragenter', function (e) {
        if (isEditable === false) return
        e.stopPropagation();
        e.preventDefault();
    });
    $(document).on('dragover', function (e) {
        if (isEditable === false) return
        e.stopPropagation();
        e.preventDefault();
    });
    $(document).on('drop', function (e) {
        if (isEditable === false) return
        e.stopPropagation();
        e.preventDefault();
    });
    // #endregion

    // #region 削除ボタン
    $(document).on("click", ".delete_file", function () {
        if (isEditable === false) return
        var dir_kind = $(this).data('dir_kind')
        var file_name = $(this).data('file_name')
        console.log(dir_kind);
        var dic_cd = $("#dic_cd").val()
        console.log(dic_cd);

        var btn_delete = $(this);

        if (dir_kind == 1) {
            // 既存のファイルを削除する
            div_icon_empty(btn_delete);
            removeFileFromDataTransfer(file_name)
            $('#Delete_files').val($('#Delete_files').val() + file_name + ":");
        }
        else {
            var work_dir = $('#work_dir').val();
            // 新しいファイルを削除する
            $.ajax({
                type: "POST",
                url: `${baseUrl}Base/DeleteFile?dic_cd=${dic_cd}&work_dir=${work_dir}&file_name=${file_name}`,
                //async: false,
                success: function (ret, status, xhr) {
                    if (xhr.status === 200) {
                        // ステータスコードが 200 の場合
                        div_icon_empty(btn_delete);
                    } else if (xhr.status === 202) {
                        // ステータスコードが 200 以外の場合
                        div_icon_empty(btn_delete);
                    } else {

                    }
                    removeFileFromDataTransfer(file_name)
                },
                error: function (e) {
                    //レスポンスが返って来ない場合
                    console.log("削除失敗：　" + e);
                },
            });
        }
    });

    $(document).on("click", ".comment_delete_file", function () {
        // if (isEditable === false) return
        var dir_kind = $(this).data('dir_kind')
        var file_name = $(this).data('file_name')
        console.log(dir_kind);
        var dic_cd = $("#comment_dic_cd").val()
        console.log(dic_cd);

        var btn_delete = $(this);

        if (dir_kind == 1) {
            // 既存のファイルを削除する
            comment_div_icon_empty(btn_delete);
            removeFileFromCommentDataTransfer(file_name)
            $('#Delete_files').val($('#Delete_files').val() + file_name + ":");
        }
        else {
            var work_dir = $('#comment_work_dir').val();
            // 新しいファイルを削除する
            $.ajax({
                type: "POST",
                url: `${baseUrl}Base/DeleteFile?dic_cd=${dic_cd}&work_dir=${work_dir}&file_name=${file_name}`,
                //async: false,
                success: function (ret, status, xhr) {
                    if (xhr.status === 200) {
                        // ステータスコードが 200 の場合
                        comment_div_icon_empty(btn_delete);
                    } else if (xhr.status === 202) {
                        // ステータスコードが 200 以外の場合
                        comment_div_icon_empty(btn_delete);
                    } else {

                    }
                    removeFileFromCommentDataTransfer(file_name)
                },
                error: function (e) {
                    //レスポンスが返って来ない場合
                    console.log("削除失敗：　" + e);
                },
            });
        }
    });
    // #endregion

    $('#drag_area').on('dragover', function (e) {
        if (isEditable === false) return
        e.preventDefault(); // Prevent default behavior to allow drop
        $(this).addClass('drag-over');
    });

    $('#drag_area').on('dragleave', function () {
        if (isEditable === false) return
        $(this).removeClass('drag-over');
    });
    $('#drag_area').on('drop', function () {
        if (isEditable === false) return
        $(this).removeClass('drag-over');
    });

    $('#comment_drag_area').on('dragover', function (e) {
        e.preventDefault(); // Prevent default behavior to allow drop
        $(this).addClass('drag-over');
    });

    $('#comment_drag_area').on('dragleave', function () {
        $(this).removeClass('drag-over');
    });
    $('#comment_drag_area').on('drop', function () {
        $(this).removeClass('drag-over');
    });
});

function add_div_icon(filename, drag) {
    var count = $('#div_icon').find('.div_icon_child').length;
    var svg = filename.split('.').pop() + ".svg";

    var html = '<div id="div_icon_' + count + '" class="div_icon_child dropdown fileAreaInnerWidth">' +
        '<button class="border-0 p-0 dropdown-toggle btn_file fileAreaInnerWidth" type="button" data-bs-toggle="dropdown" aria-expanded="false" style="background-color: var(--bs-card-bg);">' +
        '<div class="div_tooltip" data-toggle="tooltip" data-placement="top" title="' + filename + '">' +
        '<div class="div_img_file bg-light p-2">' +
        '<img src="' + baseUrl + 'images/file-icons/' + svg + '" alt="icon">' +
        '</div>' +
        '<div class="text-wrap main_files">' + filename + '</div>' +
        '<div class="">';
    if (drag) {
        html += (
            '<progress class="fileAreaInnerWidth" value="0" id="prog_' + filename + '" max="100"></progress>'
        );
    }
    html += (
        '</div>' +
        '</div>' +
        '</button>' +
        '<ul class="dropdown-menu fileAreaInnerWidth text-center">' +
        '<button class="dropdown-item delete_file" type="button" role="button" id="delete_' + count + '" data-dir_kind="2"  data-file_name="' + filename + '">削除</button>' +
        '</ul>' +
        '</div>');
    $('#div_icon').append(html);
    $('.btn_file').on('mouseenter', function (e) {
        $(this).find('.div_img_file').addClass('border');
        $(this).find('.div_img_file').addClass('border-info');
    });
    $('.btn_file').on('mouseleave', function (e) {
        $(this).find('.div_img_file').removeClass('border');
        $(this).find('.div_img_file').removeClass('border-info');
    });

}

function add_comment_div_icon(filename, drag) {
    var count = $('#comment_div_icon').find('.comment_div_icon_child').length;
    var svg = filename.split('.').pop() + ".svg";

    var html = '<div id="comment_div_icon_' + count + '" class="comment_div_icon_child dropdown fileAreaInnerWidth">' +
        '<button class="border-0 p-0 dropdown-toggle comment_btn_file fileAreaInnerWidth" type="button" data-bs-toggle="dropdown" aria-expanded="false" style="background-color: var(--bs-card-bg);">' +
        '<div class="comment_div_tooltip" data-toggle="tooltip" data-placement="top" title="' + filename + '">' +
        '<div class="comment_div_img_file bg-light p-2">' +
        '<img src="' + baseUrl + 'images/file-icons/' + svg + '" alt="icon">' +
        '</div>' +
        '<div class="text-wrap main_files">' + filename + '</div>' +
        '<div class="">';
    if (drag) {
        html += (
            '<progress class="fileAreaInnerWidth" value="0" id="prog_' + filename + '" max="100"></progress>'
        );
    }
    html += (
        '</div>' +
        '</div>' +
        '</button>' +
        '<ul class="dropdown-menu fileAreaInnerWidth text-center">' +
        '<button class="dropdown-item comment_delete_file" type="button" role="button" id="delete_' + count + '" data-dir_kind="2"  data-file_name="' + filename + '">削除</button>' +
        '</ul>' +
        '</div>');
    $('#comment_div_icon').append(html);
    $('.comment_btn_file').on('mouseenter', function (e) {
        $(this).find('.comment_div_img_file').addClass('border');
        $(this).find('.comment_div_img_file').addClass('border-info');
    });
    $('.comment_btn_file').on('mouseleave', function (e) {
        $(this).find('.comment_div_img_file').removeClass('border');
        $(this).find('.comment_div_img_file').removeClass('border-info');
    });

}

function div_icon_empty(btn_delete) {
    var arr_no = btn_delete.attr('id').split('_');
    var no = arr_no[arr_no.length - 1];

    $('#div_icon_' + no).empty();
    $('#div_icon_' + no).addClass('d-none');

}

function comment_div_icon_empty(btn_delete) {
    var arr_no = btn_delete.attr('id').split('_');
    var no = arr_no[arr_no.length - 1];
    $('#comment_div_icon_' + no).empty();
    $('#comment_div_icon_' + no).addClass('d-none');

}

function removeFileFromDataTransfer(file_name) {
    for (var i = 0; i < _dataTransfer.items.length; i++) {
        console.log("前回選択ドロップからファイル削除開始");
        if (_dataTransfer.files[i].name == file_name) {
            console.log(_dataTransfer.files[i].name);
            _dataTransfer.items.remove(i);
        }
    }
}

function removeFileFromCommentDataTransfer(file_name) {
    for (var i = 0; i < _commentDataTransfer.items.length; i++) {
        console.log("前回選択ドロップからファイル削除開始");
        if (_commentDataTransfer.files[i].name == file_name) {
            console.log(_commentDataTransfer.files[i].name);
            _commentDataTransfer.items.remove(i);
        }
    }
}

// #region ダウンロードボタン
$(document).on("click", ".download_file", function () {
    var dic_cd = $('#dic_cd').val();
    var dir_no = $('#dir_no').val();
    var file_name = $(this).data('file_name');
    var dir_no_child = $(this).data('dir_no_child');//フォルダがネストの時に使用
    var url
    if (dir_no_child == undefined) {
         url = baseUrl + "Base/DownloadFile?" + "dic_cd=" + dic_cd + "&dir_no=" + dir_no + "&file_name=" + file_name;
    } else {
        url = baseUrl + "Base/DownloadFile?" + "dic_cd=" + dic_cd + "&dir_no=" + dir_no + '\\' + dir_no_child + "&file_name=" + file_name ;
    }
    //指定したURLからファイルをダウンロードする
    funcFileDownload(url, file_name);
});

$(document).on("click", ".comment_download_file", function () {
    var dic_cd = $('#comment_dic_cd').val();
    var comment_no = $(this).data('comment_no');
    var dir_no = $('#dir_no').val() + '\\' + comment_no;
    var file_name = $(this).data('file_name');
    var dir_no_child = $(this).data('dir_no_child');//フォルダがネストの時に使用
    var url
    if (dir_no_child == undefined) {
         url = baseUrl + "Base/DownloadFile?" + "dic_cd=" + dic_cd + "&dir_no=" + dir_no + "&file_name=" + file_name;
    } else {
        url = baseUrl + "Base/DownloadFile?" + "dic_cd=" + dic_cd + "&dir_no=" + dir_no + '\\' + dir_no_child + "&file_name=" + file_name ;
    }
    //指定したURLからファイルをダウンロードする
    funcFileDownload(url, file_name);
});

