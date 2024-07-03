$(function () {
    var deadline_set = $("#s_deadline").val();
    if(deadline_set == 0) {
        $("#deadline_date_area").css('display', 'none');
    }
    if(deadline_set == 1) {
        $("#deadline_date_area").css('display', 'block')
    }
    $(".back").on('click', function () {
        var path_dir_delete = $('#work_dir').val()
        var dic_cd = $("#dic_cd").val()
        $.ajax({
            type: "GET",
            url: `${baseUrl}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        })

        window.location = baseUrl + "Todo/Index"
    })

    $(".btnComment").on('click', function(){
        var id = $('#id').val();
        window.location.href = baseUrl + `Todo/CommentList?id=${id}`
    })

    $('.btnCreate').on('click', function (e) {
        $('.loading').show()

        $('#todoForm').trigger("submit");
    })

    $('#todoForm').on('submit', function (e) {
        if (!$(this).valid()) {
            $('.loading').hide()
        }
    })

    $('.btnUpdate').on('click', function (e) {
        $('.loading').show()

        $('#todoForm').trigger("submit")
    })

    $("#MyStaffList").selectize({
        plugins: ["remove_button"]
    })

    // checkHasFile();
    
})

function showEndDate() {
    var deadline_set = $("#s_deadline").val();
    if(deadline_set == 0) {
        $("#deadline_date_area").css('display', 'none');
    }
    if(deadline_set == 1) {
        $("#deadline_date_area").css('display', 'block')
    }
}

// function checkHasFile() {
//     var $divIcon = $('#div_icon');
//     if ($divIcon.children().length === 0) {
//         console.log('The div does not contain any tags.');
//     } else {
//         console.log('The div contains tags.');
//     }
// }