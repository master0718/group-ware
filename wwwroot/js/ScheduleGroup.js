let calendar = null;
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
    });

    $('#add_schedule').click(function () {
        if (calendar != null) {
            var currentView = calendar.view;
            var startDate = currentView.activeStart;

            if (viewMode == 'GroupWeek')
                window.location.href = "CreateGroupWeek?start_date=" + moment(startDate).format('YYYY-MM-DD');
            else
                window.location.href = "CreateGroupDay?start_date=" + moment(startDate).format('YYYY-MM-DD');
        }
    })

    $('#group_list').on('change', function () {
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
        url: `ScheduleListGroup?filter=${filter}`,
        success: function (result) {
            console.log(result);
            if (result.staffList != undefined)
                updateOnResponse(result.user_id, result.staffList, result.scheduleList, date_from)
        },
        error: function (xhr) {
            console.log("Error:" + xhr.responseText);
        }
    })
}

function updateOnResponse(user_id, staffList, scheduleList, initialDate) {    
    var resourcesData = [];
    var eventsData = [];

    if (staffList != null && staffList.length > 0) {
        staffList.forEach(element => {
            resourcesData.push({ 'id': user_id == element.staf_cd ? "1-" + user_id : "2-" + element.staf_cd, 'title': element.staf_name })
        })
        if (scheduleList != null && scheduleList.length > 0) {
            loadCalendarEventsData(user_id, 'staf', scheduleList, eventsData)
        }
    }

    let slotDuration;
    let viewMode_ = null
    let buttons
    if (viewMode == 'GroupWeek') {
        slotDuration = { days: 1 }; // Set slot duration to 1 day
        viewMode_ = 'resourceTimelineWeek'
        buttons = 'prev,prev_day,next_day,next today'
    }
    else {
        slotDuration = { hours: 1 }; // Set slot duration to 1 hour
        viewMode_ = 'resourceTimelineDay'
        buttons = 'prev,prev_day,next_day,next today'
    }
    var calendarEl = document.getElementById('calendar');
    calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: viewMode_,
        initialDate: initialDate,
        firstDay: new Date().getDay(),

        slotDuration: slotDuration, // Set the slot duration to 1 day
        slotLabelInterval: slotDuration, // Display slot labels for each day

        slotMinTime: '07:00:00', // Set the minimum time to display
        slotMaxTime: '20:00:00', // Set the maximum time to display

        slotLaneClassNames: function () {
            return 'weekday-only';
        },
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
        views: {
            resourceTimelineWeek: {
                duration: { days: 7 },
                dateIncrement: { days: 1 },
                titleFormat: (info) => {
                    var date = new Date(info.date.year, info.date.month, info.date.day);
                    const formattedDate = `${info.date.year} 年 ${info.date.month+1} 月 ${info.date.day} 日 `;
                    const weekday = ['日', '月', '火', '水', '木', '金', '土'][date.getDay()];
                    date.setDate(date.getDate() + 6);
                    const formattedEndDate = `${date.getDate()} 日 `;
                    const weekdayEnd = ['日', '月', '火', '水', '木', '金', '土'][date.getDay()];

                    return `${formattedDate}(${weekday})  – ${formattedEndDate}(${weekdayEnd})`;
                },
            },
            resourceTimelineDay: {
                titleFormat: (info) => {
                    var date = new Date(info.date.year, info.date.month, info.date.day);
                    const formattedDate = `${info.date.year} 年 ${info.date.month+1} 月 ${info.date.day} 日 `;
                    const weekday = ['日', '月', '火', '水', '木', '金', '土'][date.getDay()];
                    return `${formattedDate}(${weekday})`;
                },
            },            
        },
        buttonText: {
            today: '今日',
            prev: '前週',
            next: '翌週'
        },
        editable: true,
        eventResourceEditable: false,
        eventDurationEditable: false, // Disable Resize
        height: '500px',
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
        slotLabelContent: function(arg) {
            if (arg.view.type == 'resourceTimelineWeek') {
                let dayNumber = arg.date.getDate();
                let dayOfWeek = arg.date.toLocaleDateString('ja', {weekday: 'short'});
    
                return `${dayNumber} (${dayOfWeek})`;
            }
            else {
                return arg.text;
            }
        },
        resourceAreaWidth: "12%",
        resourceAreaHeaderContent: "",
        resourceLabelContent: function (arg) {
            var description = arg.resource._resource.title;

            return {
                html: '<div class="resource-description">' + description + '</div>'
            };
        },

        resources: resourcesData,
        //resourceOrder: 'sort_id',
        events: eventsData,
        eventClassNames: 'event',
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

            var action = null
            if (viewMode == 'GroupWeek')
                action = "EditGroupWeek"
            else
                action = "EditGroupDay"
            window.location.href = `${action}?schedule_no=${schedule_no}&start_date=${ moment(startDate).format('YYYY-MM-DD') }`
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
