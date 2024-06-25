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

    $('.btnCreate').on('click', function (e) {
        $('.loading').show()
        
        $('#workFlowForm').trigger("submit");
    })

    $('#workFlowForm').on('submit', function (e) {
        if (!$(this).valid()) {
            $('.loading').hide()
        }
    })


    $('.btnUpdate').on('click', function (e) {
        $('.loading').show()

        $('#workFlowForm').trigger("submit")
    })

})