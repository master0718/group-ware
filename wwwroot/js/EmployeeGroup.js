$(function() {
    
    $('.iframe').colorbox(
        {
            iframe: true,
            width: "500px", height: "400px",
            opacity: 0.5
        });
    $('.iframe-add').colorbox(
        {
            iframe: true,
            width: "700px", height: "450px",
            opacity: 0.5
        });
    $('input#selectAllFile').on({
        change: function () {
            if ($(this).is(':checked')) {
                $('input.selectFile').prop('checked', true);
            }
            else {
                $('input.selectFile').prop('checked', false);
            }
        }
    });
}); 