var type_dlg = 0;
var selected_id = -1;

$(function () {
    // from Controller

    $('#add_memo').click(function () {
        if (viewMode == 'Memo_sent')
            window.location.href = "CreateSent";
        else
            window.location.href = "CreateReceived";
    })
})

function FilterChanged() {
    $("#selectionForm").trigger("submit");
}
