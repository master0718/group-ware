let calendar = null

$(function () {
    // from Controller
    var startDate = $('#startDate').val()

    let baseDay = moment(startDate)
    $.ajax({
        type: "GET",
        url: baseUrl + "Base/GetHolidays",
        success: function (result) {
            holidays = result;
            navigate("", baseDay)

        },
        error: function (xhr) {
        }
    })

    $('#add_schedule').click(function () {
        if (calendar != null) {
            var currentView = calendar.view;
            var startDate = currentView.currentStart;

            if (viewMode == 'PersonMonth')
                window.location.href = "CreatePersonMonth?start_date=" + moment(startDate).format('YYYY-MM-DD');
            else
                window.location.href = "CreatePersonMonth2?start_date=" + moment(startDate).format('YYYY-MM-DD');
        }
    })

    $('#staff_list').on('change', function () {
        if (calendar != null) {
            var currentView = calendar.view;
            var activeStart = currentView.activeStart;

            if (activeStart.getDate() != 1) {
                baseDay = new Date(activeStart.getFullYear(), activeStart.getMonth() + 1, 1); // Calculate the start day of the next month
            }
            else {
                baseDay = activeStart;
            }

            navigate($(this).val(), baseDay)
        }
    })
})

function navigate(filter, baseDay) {
    var date_from = new moment(baseDay).format('YYYY-MM-DD')

    $.ajax({
        method: "get",
        url: `ScheduleListPerson?filter=${filter}`,
        success: function (result) {
            console.log(result);
            updateOnResponse(result.scheduleList, date_from)
        },
        error: function (xhr) {
            console.log("Error:" + xhr.responseText);
        }
    })
}

function updateOnResponse(scheduleList, initialDate) {    
    var eventsData = [];
    eventsData.push({
        'resourceId': 0,
        'startTime': "23:00",
        'endTime': "23:00",
        'is_add': 1
    })
    if (viewMode == 'PersonMonth2') {
        scheduleList = scheduleList.filter(s=>s.schedule_type == 100);
    }
    loadCalendarEventsData(0, 'staf', scheduleList, eventsData)

    var initialViewOption = 'dayGridMonth'; // Default initialView option
    if (viewMode == 'PersonMonth2') {
        initialViewOption = 'twoMonth'; // Set initialView to 'twoMonth' if viewmode is 'multi'
        $('body').addClass('twoMonth')
    }

    let buttons = 'prev,next today'
    var calendarEl = document.getElementById('calendar');
    if (calendar != null) {
        calendar.destroy();
    }
    calendar = new FullCalendar.Calendar(calendarEl, {
        initialDate: initialDate,
        initialView: initialViewOption,
        views: {
            twoMonth: {
                type: 'multiMonth',
                multiMonthMaxColumns: 1,
                duration: { months: 2 }
            }
        },
        locale: 'ja',
        timeZone: 'local',
        //aspectRatio: 1.5,
        // headerToolbar: null,
        headerToolbar: {
            left: '',
            center: 'title',
            right: buttons
        },
        buttonText: {
            today: '今日',
            prev: '前月',
            next: '翌月'
        },
        editable: true,
        eventResourceEditable: false,
        eventDurationEditable: false, // Disable Resize        
        slotLabelClassNames: function (data) {
            var localDate = data.date;
            var year = localDate.getFullYear();
            var month = String(localDate.getMonth() + 1).padStart(2, '0');
            var day = String(localDate.getDate()).padStart(2, '0');
            var stringDate = year + '-' + month + '-' + day;

            if ($.inArray(stringDate, holidays) !== -1) {
                return 'ast-holiday';
            }
        },
        dayCellContent: function (arg) {
            let dayNumber = arg.date.getDate();
            let dayMonth = arg.date.getMonth() + 1;
            return `${dayMonth}/${dayNumber}`;
        },          
        events: eventsData,
        eventContent: function (arg) {
            var props = arg.event.extendedProps;
            let startTime = arg.event.start;
            let endTime = arg.event.end;
            let from = moment(startTime, 'YYYY/MM/DD HH:mm').format('YYYY/MM/DD HH:mm');
            let to = moment(endTime, 'YYYY/MM/DD HH:mm').format('YYYY/MM/DD HH:mm');

            if (props.is_add) {
                return {
                    html: `<div class="cell-add-month" data-from="${from}"><i class="bi bi-plus ic-cell-add"></i></div>`
                };
            } else {
                var contentStyle = props.bkcolor == undefined ? `` : ` style="background-color:${props.bkcolor}"`
                let schedule_no = props.schedule_no;
                let place_cd = props.place_cd;
        
                // Format the start and end times to display in the format of 7.00-8.00
                let formattedStartTime = startTime.toLocaleTimeString('ja', {hour: 'numeric', minute: '2-digit'});
                let formattedEndTime = endTime.toLocaleTimeString('ja', {hour: 'numeric', minute: '2-digit'});

                if (arg.event.extendedProps.is_private) {
                    return {
                        html: `<div class="fc-content"${contentStyle} data-from="${from}" data-to="${to}" data-schedule_no="${schedule_no}" data-place_cd="${place_cd}">
                                    <div class="fc-date">${formattedStartTime}-${formattedEndTime}</div>
                                    <div class="px-1">
                                        <span class="fc-type px-1" style="background-color:${arg.event.extendedProps.typecolor}; height:fit-content;">${arg.event.extendedProps.typename}</span>
                                        <i class="bi bi-lock-fill fc-lock"></i>
                                        <span class="fc-title" style="color:${arg.event.extendedProps.typecolor}">${arg.event.title}</span>
                                    </div>
                                </div>`
                    };
                }
                else {
                    return {
                        html: `<div class="fc-content"${contentStyle} data-from="${from}" data-to="${to}" data-schedule_no="${schedule_no}" data-place_cd="${place_cd}">
                                    <div class="fc-date">${formattedStartTime}-${formattedEndTime}</div>
                                    <div class="px-1">
                                        <span class="fc-type px-1" style="background-color:${arg.event.extendedProps.typecolor}; height:fit-content;">${arg.event.extendedProps.typename}</span>
                                        <span class="fc-title" style="color:${arg.event.extendedProps.typecolor}">${arg.event.title}</span>
                                    </div>
                                </div>`
                    };
                }
            }
        },
        eventDrop: function(event) {
            let e = event.event;
            $.ajax({
                method: "POST",
                url: baseUrl + "ScheduleFacility/UpdateDuration",
                data: {
                    schedule_no: e.extendedProps.schedule_no,
                    start: e.startStr,
                    end: e.endStr
                },                
                success: function (result) {
                    console.log(result);
                },
                error: function (xhr) {
                    console.log("Error:" + xhr.responseText);
                }
            })
        },
        eventClick: function (info) {
            var schedule_no = info.event.extendedProps.schedule_no
            var currentView = calendar.view;
            var startDate = currentView.currentStart;

            var action = null;
            if (viewMode == 'PersonWeek')
                action = "EditPersonWeek";
            else if (viewMode == 'PersonMonth')
                action = "EditPersonMonth";
            else
                action = "EditPersonMonth2";

            if (schedule_no == undefined) {
                schedule_no = 0
                currDate = info.event.start;
                window.location.href = `${action}?schedule_no=${schedule_no}&start_date=${moment(startDate).format('YYYY-MM-DD')}&curr_date=${moment(currDate).format('YYYY-MM-DD')}`
            } else {
                window.location.href = `${action}?schedule_no=${schedule_no}&start_date=${moment(startDate).format('YYYY-MM-DD')}`
            }
        },
        
        dayCellClassNames: function (fn) {
            $(".fc-content").each(function () {
                var duplicated = false;
                var thiz = $(this);
                var schedule_no = thiz.data('schedule_no');
                var place_cd = thiz.data('place_cd');
                var from = moment(thiz.data('from'), 'YYYY/MM/DD HH:mm');
                var to = moment(thiz.data('to'), 'YYYY/MM/DD HH:mm');

                $(".fc-content").each(function () {
                    var thiz1 = $(this);

                    if (schedule_no == thiz1.data('schedule_no') && place_cd == thiz1.data('place_cd')) return; // itself
                    //if (place_cd != thiz1.data('place_cd')) return; // different place

                    var item_from = moment(thiz1.data('from'), 'YYYY/MM/DD HH:mm');
                    var item_to = moment(thiz1.data('to'), 'YYYY/MM/DD HH:mm');
                    if (item_from.isAfter(to) || item_to.isBefore(from)) return;
                    duplicated = true;
                    return;
                });

                if (duplicated) {
                    //thiz.attr('duplicated', true);
                    if (thiz.find(".fc-duplicated").length == 0) {
                        thiz.find(".fc-date").prepend('<i class="bi bi-exclamation-triangle-fill fc-duplicated" style="color:red"></i>');
                    }
                }
            });
            $("a.fc-event:has(.ic-cell-add)").css({
                'width': 'fit-content'
            });
        }
    });

    calendar.render();
}
