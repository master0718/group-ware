$(function () {
    
    $(".back").on('click', function () {
        if (isEditable && $(this).hasClass('back-edit')) {
            isEditable = false
            hasCopied = false
            updateOnEditableChange()
        } else {

            var path_dir_delete = $('#work_dir').val()
            var dic_cd = $("#dic_cd").val()
            $.ajax({
                type: "GET",
                url: `${baseUrl}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
            })

            window.location = `${baseUrl}Schedule/${viewMode}?start_date=${startDate}`
        }
    })

    var staffListOfGroup = {}
    $("[data-staff_cds]").each(function () {
        var groupCd = $(this).val()
        var staffCds_ = String($(this).data("staff_cds"))
        var staffCds = staffCds_.split(",")
        staffListOfGroup[groupCd] = staffCds
    })

    $("#MyStaffList").selectize({
        plugins: ["remove_button"]
    })
    $("#MyPlaceList").selectize({
        plugins: ["remove_button"]
    })

    $('#MyPlaceList').on('change', function (e) {
        if ($('#MyPlaceList').val().length > 0) {
            $(".field-validation-error").text("");
        }
    })    

    var programmaticallyChanging = false
    $('#MyStaffList').on('change', function (e) {
        if (!programmaticallyChanging) {
            var selectize = $("#MyStaffList").selectize()
            var selectize_ = selectize[0].selectize
            var newValue = []
            var curValue = selectize_.getValue()
            for (var i = 0; i < curValue.length; i++) {
                var val = curValue[i]
                if (val.startsWith("G")) {
                    var g = staffListOfGroup[val]
                    for (var j = 0; j < g.length; j++) {
                        if (!g[j]) continue;
                        if (newValue.indexOf("S-" + g[j]) == -1)
                            newValue.push("S-" + g[j])
                    }
                } else {
                    newValue.push(val)
                }
            }
            programmaticallyChanging = true
            selectize_.setValue(newValue)
            programmaticallyChanging = false
        }
    })

    $('.btnEdit').on('click', function () {
        isEditable = true
        updateOnEditableChange()
    })

    $('.btnCopy').on('click', function () {
        hasCopied = true
        isEditable = true
        updateOnEditableChange()
    })

    function updateOnEditableChange() {
        if (isEditable) {
            $('#pageTitle').text(hasCopied ? '予約の登録' : '予約の編集')

            $('.btnEdit').attr('hidden', true)
            $('.btnDelete').attr('hidden', true)
            $('.btnCopy').attr('hidden', true)
            $('#schedule_type').removeAttr('disabled')
            $('#title').removeAttr('readonly')
            $('#memo').removeAttr('readonly')
            $('#is_private').removeAttr('disabled')
            $('#MyStaffList').removeAttr('disabled')
            $('#MyPlaceList').removeAttr('disabled')
            $('#MyStaffList')[0].selectize.enable()
            $('#MyPlaceList')[0].selectize.enable()

            $('#File').removeAttr('disabled')
            $('#drag_area').parent().addClass('dropArea')
            $("[name='repeatTypeRadio']").removeAttr('disabled')
            $("[name='repeatLimitRadio']").removeAttr('disabled')
            $(".btn_file").removeClass('download_file')
            $(".btn_file").attr('data-bs-toggle', 'dropdown')

            var repeatType = $("[name='repeatTypeRadio']:checked").val()
            if (repeatType == 3) {
                $("#list-week").removeAttr('disabled')
            } else if (repeatType == 4) {
                $("#list-day").removeAttr('disabled')
            }

            $('#create_info').attr('hidden', true)
            $('#update_info').attr('hidden', true)

            if (hasCopied) {
                $('#scheduleForm').attr('action', '/groupware/Schedule/Create')
                $('.btnSave').html('<i class="bi bi-plus"></i> 新規登録')
            } else {
                $('#scheduleForm').attr('action', '/groupware/Schedule/Edit')
                $('.btnSave').html('<i class="bi bi-floppy"></i> 保存')
            }

            $('.btnSave').removeAttr('hidden')

        } else {
            $('#pageTitle').text('詳細内容')

            $('.btnEdit').removeAttr('hidden')
            $('.btnDelete').removeAttr('hidden')
            $('.btnCopy').removeAttr('hidden')
            $('.btnSave').attr('hidden', true)
            $('#schedule_type').attr('disabled', true)
            $('#title').attr('readonly', true)
            $('#memo').attr('readonly', true)
            $('#is_private').attr('disabled', true)
            $('#MyStaffList').attr('disabled', true)
            $('#MyPlaceList').attr('disabled', true)
            $('#MyStaffList')[0].selectize.disable()
            $('#MyPlaceList')[0].selectize.disable()
            $('#create_info').removeAttr('hidden')
            $('#update_info').removeAttr('hidden')

            $('#File').attr('disabled', true)
            $('#drag_area').parent().removeClass('dropArea')
            $("[name='repeatTypeRadio']").attr('disabled', true)
            $("[name='repeatLimitRadio']").attr('disabled', true)
            $("#list-week").attr('disabled', true)
            $("#list-day").attr('disabled', true)
            $(".btn_file").addClass('download_file')
            $(".btn_file").dropdown('hide')
            $(".btn_file").attr('data-bs-toggle', '')

            $('#time_from').timepicker('destroy')
            $('#time_to').timepicker('destroy')
            $('#start_datetime').datepicker('destroy')
            $('#end_datetime').datepicker('destroy')
        }

        initDate()
        initTime()
    }

    function initRepeatType() {
        $("[name='repeatTypeRadio']").on('change', function () {
            showHideDateTimeControls($(this))
        })

        var repeatType = $("#repeat_type").val()
        var selector = `[name='repeatTypeRadio'][value='${repeatType}']`
        $(selector).attr('checked', true)

        showHideDateTimeControls()
        initWeeklySelector()
        initMonthlySelector()
    }

    function initRepeatLimitDate() {
        $("[name='repeatLimitRadio']").on('change', function () {
            updateRepeatLimitControls($(this))
        })
        updateRepeatLimitControls()
        if ($("#repeat_date_from").val() != "") {
            $("[name='repeatLimitRadio']").attr("checked", true)
        }
    }

    function showHideDateTimeControls($thiz) {
        var repeatType = $thiz == null ? $("#repeat_type").val() : $thiz.val()
        if (repeatType == 0) { // none
            //$("#input-time-from-wrapper").addClass('d-none')
            //$("#input-time-to-wrapper").addClass('d-none')
            $("#input-date-from-wrapper").removeClass('d-none')
            $("#input-date-to-wrapper").removeClass('d-none')
            $("[name='repeatLimitRadio']").attr('disabled', true)
        } else {
            //$("#input-time-from-wrapper").removeClass('d-none')
            //$("#input-time-to-wrapper").removeClass('d-none')
            $("#input-date-from-wrapper").addClass('d-none')
            $("#input-date-to-wrapper").addClass('d-none')
            if (isEditable)
                $("[name='repeatLimitRadio']").removeAttr('disabled')
        }
        updateRepeatLimitControls()

        if (repeatType == 3) { // weekly
            if (isEditable)
                $("#list-week").removeAttr('disabled')
        } else {
            $("#list-week").attr('disabled', true)
        }

        if (repeatType == 4) { // monthly
            if (isEditable)
                $("#list-day").removeAttr('disabled')
        } else {
            $("#list-day").attr('disabled', true)
        }
    }

    function initWeeklySelector() {
        var repeatType = $("#repeat_type").val()
        if (repeatType == 3) { // weekly
            var every_on = $("#every_on").val()
            $(`#list-week option[value=${every_on}]`).attr('selected', true)
        }
    }

    function initMonthlySelector() {
        var repeatType = $("#repeat_type").val()
        if (repeatType == 4) { // monthly
            var every_on = $("#every_on").val()
            $(`#list-day option[value=${every_on}]`).attr('selected', true)
        }
    }

    function initTime() {
        var timeFrom
        var timeTo

        var repeatType = $("#repeat_type").val()
        if (repeatType == 0) { // none
            var dateFrom = $('#start_datetime').attr('value')
            var dateTo = $('#end_datetime').attr('value')
            if (dateFrom.length == 10)
                timeFrom = "07:00"
            else
                timeFrom = moment(dateFrom).format("HH:mm")

            if (dateTo.length == 10)
                timeTo = "08:00"
            else
                timeTo = moment(dateTo).format("HH:mm")

        } else {
            timeFrom = $('#time_from').val()
            timeTo = $('#time_to').val()
            if (timeFrom == "") timeFrom = "07:00"
            if (timeTo == "") timeTo = "08:00"
        }

        if (isEditable) {
            $('#time_from').timepicker({
                timeFormat: 'HH:mm',
                interval: 15,
                minTime: '07:00',
                maxTime: '19:30',
                defaultTime: timeFrom,
                startTime: '07:00',
                dynamic: false,
                dropdown: true,
                scrollbar: true,
                change: changeFromTime
            });
            $('#time_to').timepicker({
                timeFormat: 'HH:mm',
                interval: 15,
                minTime: '07:00',
                maxTime: '20:30',
                defaultTime: timeTo,
                startTime: '07:00',
                dynamic: false,
                dropdown: true,
                scrollbar: true,
                change: changeToTime
            })
        } else {
            $('#time_from').val(timeFrom)
            $('#time_to').val(timeTo)
        }
    }

    function initForm() {

        $('.btnSave').on('click', function (e) {
            $('.loading').show()

            var fileNosRemove = fileRemoveList.join(',')

            $('#file_nos_remove').attr('value', fileNosRemove)

            $('#scheduleForm').trigger("submit")
        })

        $('.btnCreate').on('click', function (e) {
            $('.loading').show()

            $('#scheduleForm').trigger("submit")
        })

        $('#scheduleForm').on('submit', function (e) {
            var repeat_type = $("[name='repeatTypeRadio']:checked").val()
            $("#repeat_type").val(repeat_type)
            if (repeat_type != 0) { // repeat none

                $("#start_datetime").val('')
                $("#end_datetime").val('')

                if (repeat_type == 3) { // weekly
                    var every_on = $("#list-week").val()
                    $("#every_on").val(every_on)
                } else if (repeat_type == 4) { // monthly
                    var every_on = $("#list-day").val()
                    $("#every_on").val(every_on)
                } else {
                    $("#every_on").val('')
                }
            } else {
                var dateFrom = $('#start_datetime').val()
                var dateTo = $('#end_datetime').val()
                var timeFrom = $('#time_from').val()
                var timeTo = $('#time_to').val()
                var start_datetime = dateFrom + ' ' + timeFrom
                var end_datetime = dateTo + ' ' + timeTo
                $('#start_datetime').val(start_datetime)
                $('#end_datetime').val(end_datetime)
                $("#time_from").val('')
                $("#time_to").val('')
                $("#every_on").val('')
            }

            var selectize = $("#MyPlaceList").selectize()
            var selectize_ = selectize[0].selectize
            var newValue = []
            var curValue = selectize_.getValue()
            for (var i = 0; i < curValue.length; i++) {
                var vals = curValue[i].split("-")
                newValue.push(vals[0])
            }
            $("#MyPlaceList").val(newValue)

            if (!$(this).valid()) {
                $('.loading').hide()
            }
        })

    }

    initRepeatType()
    initRepeatLimitDate()
    initTime()
    initForm()
});

function changeFromTime(time) {
    var selectedTime = new Date(time);
    var hour = selectedTime.getHours();
    var minute = selectedTime.getMinutes();
    var from = hour * 60 + minute;
    // 07:00 => 420
    // 19:30 => 1170
    if (from < 420)
        $('#time_from').timepicker('setTime', '07:00')
    else if (from > 1170)
        $('#time_from').timepicker('setTime', '19:30')

    var repeatType = $("#repeat_type").val()
    var dateFrom = $('#start_datetime').val()
    var dateTo = $('#end_datetime').val()
    if ((repeatType == 0 && dateFrom == dateTo) || repeatType != 0) {
        var newTime = new Date(selectedTime.getTime());
        newTime.setHours(selectedTime.getHours() + 1);

        var toTime = $('#time_to').timepicker('getTime');
        selectedTime = new Date(toTime);
        hour = selectedTime.getHours();
        minute = selectedTime.getMinutes();
        var to = hour * 60 + minute;
        if (from >= to)
            $('#time_to').timepicker('setTime', newTime);
    }
}
function changeToTime(time) {
    var selectedTime = new Date(time)
    var hour = selectedTime.getHours()
    var minute = selectedTime.getMinutes()
    var to = hour * 60 + minute
    // 07:00 => 420
    // 20:30 => 1230
    if (to < 420)
        $('#time_from').timepicker('setTime', '07:00')
    else if (to > 1230)
        $('#time_from').timepicker('setTime', '20:30')

    var repeatType = $("#repeat_type").val()
    var dateFrom = $('#start_datetime').val()
    var dateTo = $('#end_datetime').val()
    if ((repeatType == 0 && dateFrom == dateTo) || repeatType != 0) {
        var newTime = new Date(selectedTime.getTime())
        newTime.setHours(selectedTime.getHours() - 1)

        var fromTime = $('#time_from').timepicker('getTime')

        selectedTime = new Date(fromTime)
        hour = selectedTime.getHours()
        minute = selectedTime.getMinutes()
        var from = hour * 60 + minute
        if (from >= to)
            $('#time_from').timepicker('setTime', newTime)
    }
}

function initDate() {
    var dateFrom = $('#start_datetime').attr('value')
    var dateTo = $('#end_datetime').attr('value')

    if (isEditable) {
        $('#start_datetime').datepicker({
            beforeShowDay: markHolidays,
            language: 'ja'
        })
        $('#end_datetime').datepicker({
            beforeShowDay: markHolidays,
            language: 'ja'
        })

        $('#date-from-icon').click(function () {
            $("#start_datetime").focus();
        })
        $('#date-to-icon').click(function () {
            $("#end_datetime").focus();
        })
    } else {
        $('#start_datetime').datepicker('destroy')
        $('#end_datetime').datepicker('destroy')
    }

    if (dateFrom == "") {
        //$('#start_datetime').val(moment().format("YYYY年M月D日"))
        //$('#end_datetime').val(moment().format("YYYY年M月D日"))
        $('#start_datetime').val(moment().format("YYYY/MM/DD"))
        $('#end_datetime').val(moment().format("YYYY/MM/DD"))
    } else {
        $('#start_datetime').val(moment(dateFrom).format("YYYY/MM/DD"))
        $('#end_datetime').val(moment(dateTo).format("YYYY/MM/DD"))
    }

    updateRepeatLimitControls()
}

function updateRepeatLimitControls($thiz) {
    if (!isEditable) {
        $('#repeat_date_from, #repeat_date_to').datepicker('destroy')
    } else {
        var repeatType = $("#repeat_type").val()
        var limitType = $thiz == null ? $("[name='repeatLimitRadio']:checked").val() : $thiz.val()
        if (repeatType == 0 || limitType == 0) {
            $('#repeat_date_from, #repeat_date_to').datepicker('destroy')
        } else {
            $('#repeat_date_from, #repeat_date_to').datepicker({
                beforeShowDay: markHolidays,
                language: 'ja'
            })
            $('#repeatdate-from-icon').click(function () {
                $("#repeat_date_from").focus();
            })
            $('#repeatdate-to-icon').click(function () {
                $("#repeat_date_to").focus();
            })
        }
    }
}