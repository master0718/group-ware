var type_dlg = 0;
var selected_id = -1;

$(function () {
    // from Controller

    $('#filter_keyword').on('keydown', function (event) {
        if (event.which == 13) {
            event.preventDefault();
            FilterChanged();
        }
    })
    $('#add_memo').click(function () {
        if (viewMode == 'Memo_sent')
            window.location.href = "CreateSent";
        else if (viewMode == 'Memo_received')
            window.location.href = "CreateReceived";
        else
            window.location.href = "CreateAll";
    })
})

function FilterChanged() {
    $("#selectionForm").trigger("submit");
}
