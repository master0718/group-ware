$(function () {

    $(".back").on('click', function () {
        var path_dir_delete = $('#work_dir').val()
        var dic_cd = $("#dic_cd").val()
        $.ajax({
            type: "GET",
            url: `${baseUrl}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        })

        window.location = baseUrl + "WorkFlow/Index"
    })
    if (!isEditable) {
        $(".delete_file").addClass('d-none');
    }

    $('.btnSave').on('click', function (e) {
        action = "submit";
        $('.loading').show()

        var fileNosRemove = fileRemoveList.join(',')

        $('#file_nos_remove').attr('value', fileNosRemove)

        $('#workFlowForm').trigger("submit")
    })

    $('.btnCreate').on('click', function (e) {
        action = "submit";
        $('.loading').show()

        $('#workFlowForm').trigger("submit")
    })

    $('#workFlowForm').on('submit', function (e) {

        var request_type = $('#sel_request_type').val()
        $('#request_type').val(request_type)

        var approver_cd = $('#sel_approver_cd').val()
        $('#approver_cd').val(approver_cd)

        if (!$(this).valid()) {
            $('.loading').hide()
        }
    })
})

function onReject() {
    $("#is_accept").val(0);
    if ($("[name='comment']").val().trim() === "") {
        $("#workFlowForm").submit();
    }
    else {
        $('.loading').show();
        $("#workFlowForm").submit();
    }
}