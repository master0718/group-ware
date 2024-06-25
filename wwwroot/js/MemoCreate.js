$(function () {

    const action = $("#memoForm").attr('action')
    function initForm() {
        if (action.endsWith("Edit") || action.endsWith("Delete")) {
            var working_msg = $("#working_msg").val()
            var finish_msg = $("#finish_msg").val()
            if (working_msg && working_msg.length > 0) {
                $("#check-working").attr("checked", true)
                $("#working_msg_").text('対応します　済　　' + working_msg)
            } else {
                $("#working_msg_").hide()
            }
            if ($("#state").val() == "3") {
                $("#check-finish").attr("checked", true)
                $("#finish_msg_").text('処理済　　　済　　' + finish_msg)
            } else {
                $("#finish_msg_").hide()
            }

            var data = {
                memo_no: $("#memo_no").val(),
                // state: 1
            }
            var url = $("#url_update_read").val()

            $.ajax({
                method: "get",
                url: url,
                data: data,

                success: function (result) {
                    // console.log("updated")
                },
                error: function (xhr) {
                    console.log("Error: " + xhr.responseText)
                }
            })
        }
    }

    if (!action.endsWith("Delete")) {
        $("#memoForm").validate({
            rules: {
                receiver: "required",
                phone: {
                    regex: /^[-0-9]+$/
                },
                content: {
                    required: true,
                    maxlength: 255
                },
            },
            messages: {
                receiver: "宛先は必須項目です。",
                phone: {
                    regex: "電話番号は半角数字と半角ハイフンのみ入力可能です。",
                },
                content: {
                    required: "伝言は必須項目です。",
                    maxlength: "255文字以下で入力してください。",
                },
            },
        })
    }

    $('#memoForm').submit(function () {

        $('#working').val($('#check-working').is(":checked") ? 1 : 0)
        $('#finish').val($('#check-finish').is(":checked") ? 1 : 0)

        var receiver_str = $('#receiver').val()
        if (receiver_str == null || receiver_str == "") {
            var errReceiver = $('[data-valmsg-for="receiver_cd"]');
            errReceiver.text("宛先は必須項目です。");
            return
        }
        var receiver_type = receiver_str[0] == 's' ? 0 : 1
        var receiver_cd = 0
        receiver_cd = receiver_str.slice(5)
        $('#receiver_cd').val(receiver_cd)
        $('#receiver_type').val(receiver_type)

        var applicant_str = $('#applicant').val()
        var applicant_type = applicant_str[0] == 's' ? 0 : 1
        var applicant_cd = 0
        applicant_cd = applicant_str.slice(5)
        $('#applicant_cd').val(applicant_cd)
        $('#applicant_type').val(applicant_type)

        return true
    })

    initForm()
})