//$(function () {
//    var mode = $('#mode').val();
//    if (mode != 1 && mode != 2) {
//        $(document).find('.BaseDialogStaff_js_remove').addClass('invisible');
//    } else {
//        $(document).find('.BaseDialogStaff_js_area_options').addClass('bg-white');
//    }
//});
$('.Report_save_comment_no').on('click', function () {
    $('#already_read_commment_no').val($(this).attr('data-comment_no'));
});
