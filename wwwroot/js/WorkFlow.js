$(function () {
    $('#keyword').on('keyup', function () {
        search($(this).val(), $('#filter_status').val())
    })

    $('#filter_status').on('change', function () {
        search($('#keyword').val(), $(this).val())
    })

    function search(text, filter_status) {

        if (filter_status == "0" && text == "") {
            $(".item-workflow").removeClass('d-none')
        } else {
            $(".item-workflow").each(function () {
                var status = $(this).data('status')
                var title = $(this).find('.item-title').html()
                var text_ = new RegExp(text, 'i')
                if (filter_status == "0") {

                    if (title.search(text_) != -1) {
                        $(this).removeClass('d-none')
                    } else {
                        $(this).addClass('d-none')
                    }

                } else {

                    if (text == "") {

                        if (status == filter_status) {
                            $(this).removeClass('d-none')
                        } else {
                            $(this).addClass('d-none')
                        }

                    } else {

                        if (title.search(text_) != -1 && status == filter_status) {
                            $(this).removeClass('d-none')
                        } else {
                            $(this).addClass('d-none')
                        }
                    }

                }

            })
        }
    }
})