
const STATUS_TEXT = ['未対応', '依頼中', '対応中', '完了']
const STATUS_BG_CLAZZ = ['bg-upcoming', 'bg-requesting', 'bg-progress', 'bg-completed']

$(function () {
    $(".task").on('click', function () {
        let itemId = $(this).attr('id').substr(6)
        window.location.href = baseUrl + `Board/Detail?id=${itemId}`
    })    
})

////function FilterChanged() {
////    $("#selectionForm").trigger("submit");
////}

!function ($) {
    "use strict";
    function t() {
        this.$body = $("body")
    }
    t.prototype.init = function () {
        $(".tasklist").each(function () {
            var sortable = Sortable.create($(this)[0], {
                group: "shared",
                animation: 150,
                ghostClass: "bg-ghost",
                
                onEnd: function (arg) {
                    let from = $(arg.from).attr('id')
                    let to = $(arg.to).attr('id')
                    let itemId = $(arg.item).attr('id').substr(6)

                    if (from == to) return

                    var url = baseUrl + `Board/UpdateStatus?board_no=${itemId}&status=${to}`
                    $.ajax({
                        method: "get",
                        url: url,
                        success: function (result) {
                            if ("ok" === result.result) {
                                var badge = $(arg.item).find(".badge")

                                var html = STATUS_TEXT[to]
                                var fromClazz = STATUS_BG_CLAZZ[from]
                                var toClazz = STATUS_BG_CLAZZ[to]

                                badge.html(html)
                                badge.removeClass(fromClazz).addClass(toClazz)

                                var fromList = $(arg.from).parent().find(".item-count")
                                var toList = $(arg.to).parent().find(".item-count")
                                var fromCount = Number(fromList.html()) - 1
                                var toCount = Number(toList.html()) + 1
                                fromList.html(fromCount)
                                toList.html(toCount)
                            }
                        },
                        error: function (xhr) {
                            alert("Error:" + xhr.responseText);
                        }
                    })
                }
            })
            console.log(sortable)
        })
    },
    $.KanbanBoard = new t,
    $.KanbanBoard.Constructor = t
}(window.jQuery),
    function () {
        "use strict";
        window.jQuery.KanbanBoard.init()
    }();
