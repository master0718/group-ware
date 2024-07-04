
const STATUS_TEXT = ['未対応', '依頼中', '対応中', '完了']
const STATUS_BG_CLAZZ = ['bg-upcoming', 'bg-requesting', 'bg-progress', 'bg-completed']

$(function () {
    $(".task").on('click', function () {
        let itemId = $(this).attr('id').substr(6)
        window.location.href = baseUrl + `Board/Update?id=${itemId}`
    })
    $('#text-searach').on('keyup', function () {
        search($(this).val(), $('#checked').val(), $('#applicant').val(), $('#category').val())
    })

    $('#checked').on('change', function () {
        search($('#text-searach').val(), $(this).val(), $('#applicant').val(), $('#category').val())
    })
    $('#applicant').on('change', function () {
        search($('#text-searach').val(), $('#checked').val(), $(this).val(), $('#category').val())
    })

    $('#category').on('change', function () {
        search($('#text-searach').val(), $('#checked').val(), $('#applicant').val(), $(this).val())
    })

    function search(text, checked, staff_cd, category_cd) {
        if (checked == "0" && staff_cd == "0" && text == "" && category_cd == "0") {
            $("li[id^='board']").removeClass('d-none')
        } else {
            $("li[id^='board']").each(function () {
                var title = $(this).find('.item-title').html()
                var item_checked = $(this).data('checked')
                var applicant_cd = $(this).data('applicant_cd')
                var this_category_cd = $(this).data('category_cd')
                //var content = $(this).find('.item-content').html()
                var text_ = new RegExp(text, 'i')
                var show_checked = false;
                var show_staff = false;
                var show_category = false;
                var show_text = false;
                if (text == "" || title.search(text_) != -1) show_text = true;
                if (checked == "0" || (checked == "1" && item_checked == "0") || (checked == "2" && item_checked == "1")) show_checked = true;
                if (staff_cd == "0" || applicant_cd == staff_cd) show_staff = true;
                if (category_cd == "0" || category_cd == this_category_cd) show_category = true;
                if (show_checked && show_text && show_staff && show_category) {
                    $(this).removeClass('d-none')
                }
                else {
                    $(this).addClass('d-none')
                }
            })
        }
    }
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
