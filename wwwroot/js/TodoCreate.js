$(function () {
    var deadline_set = $("#s_deadline").val();
    if(deadline_set == 0) {
        $("#deadline_date_area").css('display', 'none');
    }
    if(deadline_set == 1) {
        $("#deadline_date_area").css('display', 'block')
    }
    $(".back").on('click', function () {
        var path_dir_delete = $('#work_dir').val()
        var dic_cd = $("#dic_cd").val()
        $.ajax({
            type: "GET",
            url: `${baseUrl}Base/DeleteDirectory?dic_cd=${dic_cd}&work_dir=${path_dir_delete}`
        })

        window.location = baseUrl + "Todo/Index"
    })

    $('.btnCreate').on('click', function (e) {
        action = "submit";
        $('.loading').show()

        $('#todoForm').trigger("submit");
    })

    $('#todoForm').on('submit', function (e) {
        if (!$(this).valid()) {
            $('.loading').hide()
        }
    })

    $('.btnUpdate').on('click', function (e) {
        action = "submit";
        $('.loading').show()

        $('#todoForm').trigger("submit")
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

    toggleDatePicker();

    $("input[name='deadline_set']").on('change', function () {
        toggleDatePicker();
    });
})

function showEndDate() {
    var deadline_set = $("#s_deadline").val();
    if(deadline_set == 0) {
        $("#deadline_date_area").css('display', 'none');
    }
    if(deadline_set == 1) {
        $("#deadline_date_area").css('display', 'block')
    }
}

function toggleDatePicker() {
    var deadline_set = $("input[name='deadline_set']:checked").val();
    if (deadline_set == 0) {
        $("#deadline_date").prop('disabled', true);
    } else if (deadline_set == 1) {
        $("#deadline_date").prop('disabled', false);
    }
}