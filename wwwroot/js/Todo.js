$(function () {
    $('.card-body').on('click', '.todo', function() {
        let itemId = $(this).data('id');
        window.location.href = baseUrl + `Todo/Update?id=${itemId}`
    })
    
    $('#keyword').on('keydown', function(event) {
        if (event.which == 13) {
            event.preventDefault();
            filterResponse();
        }
    });

    $('#keyword').on('blur', function() {
        filterResponse();
    });
})

function filterResponse() {
    var keyword = $('#keyword').val();
    var responseStatus = $("#select-response").val();
    var deadlineSet = $("#select-deadline").val();

    $.ajax({
        method: "get",
        url: 'Todo/TodoList' + '?response_status=' + responseStatus + '&deadline_set=' + deadlineSet + '&keyword=' + keyword,
        success: function (result) {
            $('.card-body').html(result);
        },
        error: function (xhr) {
            console.log("Error:" + xhr.responseText);
        }
    })
};

