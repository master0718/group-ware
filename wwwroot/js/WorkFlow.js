$(function () {
    $(".workflow").on('click', function () {
        let itemId = $(this).attr('data-id');
        window.location.href = baseUrl + `workflow/Update?id=${itemId}`
    })
})