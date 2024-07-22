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
        var start_time, end_time;
        if (viewMode == 'GroupWeek') {
            start_time = "23:00";
            end_time = "24:00";
        } else {
            start_time = "07:00";
            end_time = "08:00";
        }

        staffList.forEach(element => {
            var id = user_id == element.staf_cd ? "1-" + user_id : "2-" + element.staf_cd;
            resourcesData.push({ 'id': id, 'title': element.staf_name })
            eventsData.push({
                'resourceId': id,
                'startTime': start_time,
                'endTime': end_time,
                'is_add': 1
            })
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
    var calendarEl = document.getElementById('calendar')
    if (calendar != null) {
        calendar.destroy();
    }
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
            var year = localDate.getFullYear();
            var month = String(localDate.getMonth() + 1).padStart(2, '0');
            var day = String(localDate.getDate()).padStart(2, '0');
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
            var props = arg.event.extendedProps;

            let startTime = arg.event.start;
            let endTime = arg.event.end;
            let from = moment(startTime, 'YYYY/MM/DD HH:mm').format('YYYY/MM/DD HH:mm');
            let to = moment(endTime, 'YYYY/MM/DD HH:mm').format('YYYY/MM/DD HH:mm');

            if (props.is_add) {
                return {
                    html: `<div class="cell-add" data-from="${from}"><i class="bi bi-plus ic-cell-add"></i></div>`
                };
            } else {
                var contentStyle = props.bkcolor == undefined ? `` : ` style="background-color:${props.bkcolor}"`
                let schedule_no = props.schedule_no;
                let place_cd = props.place_cd;

                // Format the start and end times to display in the format of 7.00-8.00
                let formattedStartTime = startTime.toLocaleTimeString('ja', { hour: 'numeric', minute: '2-digit' });
                let formattedEndTime = endTime.toLocaleTimeString('ja', { hour: 'numeric', minute: '2-digit' });

                if (props.is_private) {
                    return {
                        html: `<div class="fc-content"${contentStyle} data-from="${from}" data-to="${to}" data-schedule_no="${schedule_no}" data-place_cd="${place_cd}">
                                    <div class="fc-date">${formattedStartTime}-${formattedEndTime}</div>
                                    <div class="px-1">
                                        <span class="fc-type px-1" style="background-color:${props.typecolor}; height:fit-content;">${props.typename}</span>
                                        <i class="bi bi-lock-fill fc-lock"></i>
                                        <span class="fc-title" style="color:${props.typecolor}">${arg.event.title}</span>
                                    </div>
                                </div>`
                    };
                }
                else {
                    return {
                        html: `<div class="fc-content"${contentStyle} data-from="${from}" data-to="${to}" data-schedule_no="${schedule_no}" data-place_cd="${place_cd}">
                                    <div class="fc-date">${formattedStartTime}-${formattedEndTime}</div>
                                    <div class="px-1">
                                        <span class="fc-type px-1" style="background-color:${props.typecolor}; height:fit-content;">${props.typename}</span>
                                        <span class="fc-title" style="color:${props.typecolor}">${arg.event.title}</span>
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
            var startDate = currentView.activeStart;

            var action = null
            if (viewMode == 'GroupWeek')
                action = "EditGroupWeek"
            else
                action = "EditGroupDay"
            if (schedule_no == undefined) {
                schedule_no = 0
                currDate = info.event.start;
                window.location.href = `${action}?schedule_no=${schedule_no}&start_date=${moment(startDate).format('YYYY-MM-DD')}&curr_date=${moment(currDate).format('YYYY-MM-DD')}`
            } else {
                window.location.href = `${action}?schedule_no=${schedule_no}&start_date=${moment(startDate).format('YYYY-MM-DD')}`
            }
        },
        viewClassNames: function (fn) {
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
                    if (place_cd != thiz1.data('place_cd')) return; // different place

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

            $("a.fc-event:has(.cell-add)").removeClass("fc-h-event");
            $("a.fc-event:has(.cell-add)").css({
                'margin': '1px'
            });
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
