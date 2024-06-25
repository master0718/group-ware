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
            navigate(baseDay)

        },
        error: function (xhr) {
        }
    });

    $('#add_facility').click(function () {
        if (calendar != null) {
            var currentView = calendar.view;
            var startDate = currentView.activeStart;

            if (viewMode == 'Week')
                window.location.href = "CreateNormalWeek?start_date=" + moment(startDate).format('YYYY-MM-DD');
            else
                window.location.href = "CreateNormalDay?start_date=" + moment(startDate).format('YYYY-MM-DD');
        }
    });

    $('#keyword').on('keydown', function(event) {
        if (event.which == 13) {
            event.preventDefault();
            refreshCalendar();
        }
    });
    $('#keyword').on('blur', function() {
        refreshCalendar();
    });
    $('#place_list').on('change', function () {
        refreshCalendar();
    })    
})

function refreshCalendar() {
    if (calendar != null) {
        var currentView = calendar.view;
        baseDay = currentView.activeStart;
        navigate(baseDay);
    }    
}

function navigate(baseDay) {
    keyword = $('#keyword').val();
    var place = $('#place_list').val();

    var date_from = new moment(baseDay).format('YYYY-MM-DD');

    $.ajax({
        method: "get",
        url: 'FacilityList' + '?place=' + place + '&keyword=' + keyword,
        success: function (result) {
            console.log(result);
            if (result.placeList != undefined) {
                updateOnResponse(result.placeList, result.scheduleList, date_from);
            }
        },
        error: function (xhr) {
            console.log("Error:" + xhr.responseText);
        }
    })
}

function updateOnResponse(placeList, scheduleList, initialDate) {
    var resourcesData = [];
    var eventsData = [];
    
    if (placeList != null && placeList.length > 0) {
        placeList.forEach(item => {
            resourcesData.push({ 'id': item.place_cd, 'title': item.place_name, 'sort_id': item.sort })
        })
        if (scheduleList != null && scheduleList.length > 0) {
            loadCalendarEventsData(0, 'place', scheduleList, eventsData)
        }
    }

    let slotDuration;
    let viewMode_;
    let buttons;
    if (viewMode == 'Week') {
        slotDuration = { days: 1 }; // Set slot duration to 1 day
        viewMode_ = "resourceTimelineWeek";
        buttons = 'prev,prev_day,next_day,next today';
    }
    else {
        slotDuration = { hours: 1 }; // Set slot duration to 1 hour
        viewMode_ = "resourceTimelineDay";
        buttons = 'prev,prev_day,next_day,next today';
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

        slotLaneClassNames: function() {
            return 'weekday-only';
        },
        locale: 'ja',
        timeZone: 'local',

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
        slotLabelClassNames : function(data) {
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
        resourceLabelContent: function(arg) {
          var description = arg.resource._resource.title;

          return {
            html: '<div class="resource-description">' + description + '</div>'
          };
        },

        resources: resourcesData,
        resourceOrder: 'sort_id',
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
    
            if (viewMode == 'Week')
                window.location.href = "EditNormalWeek?schedule_no=" + schedule_no + "&start_date=" + moment(startDate).format('YYYY-MM-DD');
            else
                window.location.href = "EditNormalDay?schedule_no=" + schedule_no + "&start_date=" + moment(startDate).format('YYYY-MM-DD');
        } 
    });
      
    calendar.render();

    $('.fc-prev-button').click(function() {
        calendar.incrementDate({ day: -6 });
    });
    $('.fc-prev_day-button').click(function() {
        calendar.incrementDate({ day: -1 });
    });

    $('.fc-next_day-button').click(function() {
        calendar.incrementDate({ day: +1 });
    });
    $('.fc-next-button').click(function() {
        calendar.incrementDate({ day: +6 });
    });
}
