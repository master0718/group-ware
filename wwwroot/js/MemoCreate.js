$(function () {

    const action = $("#memoForm").attr('action')
    function initForm() {
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