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
    $('#searchForm').trigger("submit")
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