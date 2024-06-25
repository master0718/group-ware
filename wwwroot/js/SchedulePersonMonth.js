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
            var utcTimestamp = localDate.getTime() + (localDate.getTimezoneOffset() * 60000); // Convert local time to UTC timestamp
            var utcDate = new Date(utcTimestamp); // Create a new Date object with the UTC timestamp

            var year = utcDate.getFullYear();
            var month = String(utcDate.getMonth() + 1).padStart(2, '0');
            var day = String(utcDate.getDate()).padStart(2, '0');
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
            var contentStyle = arg.event.extendedProps.bkcolor == undefined ? `` : ` style="background-color:${arg.event.extendedProps.bkcolor}"`
            let startTime = arg.event.start;
            let endTime = arg.event.end;
        
            // Format the start and end times to display in the format of 7.00-8.00
            let formattedStartTime = startTime.toLocaleTimeString('ja', {hour: 'numeric', minute: '2-digit'});
            let formattedEndTime = endTime.toLocaleTimeString('ja', {hour: 'numeric', minute: '2-digit'});
        
            if (arg.event.extendedProps.is_private) {
                return {
                    html: `<div class="fc-content"${contentStyle}>
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
                    html: `<div class="fc-content"${contentStyle}>
                                <div class="fc-date">${formattedStartTime}-${formattedEndTime}</div>
                                <div class="px-1">
                                    <span class="fc-type px-1" style="background-color:${arg.event.extendedProps.typecolor}; height:fit-content;">${arg.event.extendedProps.typename}</span>
                                    <span class="fc-title" style="color:${arg.event.extendedProps.typecolor}">${arg.event.title}</span>
                                </div>
                            </div>`
                };
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

            if (viewMode == 'PersonWeek')
                window.location.href = "EditPersonWeek?schedule_no=" + schedule_no + "&start_date=" + moment(startDate).format('YYYY-MM-DD');
            else if (viewMode == 'PersonMonth')
                window.location.href = "EditPersonMonth?schedule_no=" + schedule_no + "&start_date=" + moment(startDate).format('YYYY-MM-DD');
            else
                window.location.href = "EditPersonMonth2?schedule_no=" + schedule_no + "&start_date=" + moment(startDate).format('YYYY-MM-DD');
        }
    });

    calendar.render();
}
