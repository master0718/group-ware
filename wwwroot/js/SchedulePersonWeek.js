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
            var startDate = currentView.activeStart;

            window.location.href = "CreatePersonWeek?start_date=" + moment(startDate).format('YYYY-MM-DD');
        }
    })

    $('#staff_list').on('change', function () {
        if (calendar != null) {
            var currentView = calendar.view;
            baseDay = currentView.activeStart;
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
    loadCalendarEventsData(0, 'staf', scheduleList, eventsData)

    let buttons = 'prev,prev_day,next_day,next today'
    var calendarEl = document.getElementById('calendar');
    calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: "timeGridWeek",
        initialDate: initialDate,
        firstDay: new Date().getDay(),

        slotMinTime: '07:00:00', // Set the minimum time to display
        slotMaxTime: '20:00:00', // Set the maximum time to display

        slotLaneClassNames: function () {
            return 'weekday-only';
        },
        slotDuration: '00:30:00', // Duration of each slot
        locale: 'ja',
        timeZone: 'local',
        //aspectRatio: 1.5,
        // headerToolbar: null,
        customButtons: {
            prev_day: {
                text: '前日',
            },
            next_day: {
                text: '翌日',
            }
        },
        headerToolbar: {
            left: '',
            center: 'title',
            right: buttons
        },
        titleFormat: (info) => {
            var date = new Date(info.date.year, info.date.month, info.date.day);
            const formattedDate = `${info.date.year} 年 ${info.date.month+1} 月 ${info.date.day} 日 `;
            const weekday = ['日', '月', '火', '水', '木', '金', '土'][date.getDay()];
            date.setDate(date.getDate() + 6);
            const formattedEndDate = `${date.getDate()} 日 `;
            const weekdayEnd = ['日', '月', '火', '水', '木', '金', '土'][date.getDay()];

            return `${formattedDate}(${weekday})  – ${formattedEndDate}(${weekdayEnd})`;
        },        
        buttonText: {
            today: '今日',
            prev: '前週',
            next: '翌週'
        },
        editable: true,
        eventResourceEditable: false,
        eventDurationEditable: false, // Disable Resize
        height: '720px',
        allDaySlot: false,
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
        dayHeaderContent: (arg) => {
            let dayNumber = arg.date.getDate();
            let dayOfWeek = arg.date.toLocaleDateString('ja', {weekday: 'short'});
            return `${dayNumber} (${dayOfWeek})`;
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
            var startDate = currentView.activeStart;

            window.location.href = "EditPersonWeek?schedule_no=" + schedule_no + "&start_date=" + moment(startDate).format('YYYY-MM-DD');
        }
    });

    calendar.render();

    $('.fc-prev-button').click(function () {
        calendar.incrementDate({ day: -6 });
    });
    $('.fc-prev_day-button').click(function () {
        calendar.incrementDate({ day: -1 });
    });

    $('.fc-next_day-button').click(function () {
        calendar.incrementDate({ day: +1 });
    });
    $('.fc-next-button').click(function () {
        calendar.incrementDate({ day: +6 });
    });
}
