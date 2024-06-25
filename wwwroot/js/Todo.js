$(function () {
    $(".todo").on('click', function () {
        let itemId = $(this).attr('data-id');
        window.location.href = baseUrl + `Todo/Update?id=${itemId}`
    })
})