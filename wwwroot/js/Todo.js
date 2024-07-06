$(function () {

    $(document).on('click', '.btnUpdate', function() {
        var todo_no = $(this).closest('.todo').data('id')
        window.location.href = baseUrl + `Todo/Update?todo_no=${todo_no}`
    });

    $(document).on('click', '.btnDelete', function() {
        var todo_no = $(this).closest('.todo').data('id')
        window.location.href = baseUrl + `Todo/Delete?todo_no=${todo_no}`
    });

    $('#keyword').on('keydown', function(event) {
        if (event.which == 13) {    
            event.preventDefault();
            filterResponse();
        }
    });

    $('#keyword').on('blur', function() {
        filterResponse();
    });

    checkDeadlineDate();
})

function filterResponse() {
    var keyword = $('#keyword').val();
    var responseStatus = $("#select-response").val();
    var deadlineSet = $("#select-deadline").val();

    $.ajax({
        method: "get",
        url: `${baseUrl}Todo/TodoList`,
        data: {
            response_status: responseStatus,
            deadline_set: deadlineSet,
            keyword: keyword
        },
        success: function (result) {
            $('.card-body').html(result);
            checkDeadlineDate();
        },
        error: function (xhr) {
            console.log("Error:" + xhr.responseText);
        }
    })
};

function checkDeadlineDate() {
    const deadlineElements = document.querySelectorAll('.deadline-date');

    deadlineElements.forEach(function(element) {
        const deadlineDate = new Date(element.getAttribute('data-deadline-date'));
        const currentDate = new Date();

        if (deadlineDate < currentDate) {
            element.style.color = 'red';
        }
    });
}