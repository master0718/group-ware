function loadCalendarEventsData(user_id, resourceType, scheduleList, eventsData) {
    load_(user_id, resourceType, scheduleList, eventsData, 'staf_cd')
}

function load_(user_id, resourceType, list, eventsData, cdName) {
    if (list != null && list.length > 0) {
        list.forEach((element, index) => {

            var resourceId;
            if (resourceType == 'staf') {
                if (user_id == 0) resourceId = element[cdName]
                else resourceId = element[cdName] == user_id ? "1-" + user_id : "2-" + element[cdName]
            } else {
                resourceId = element.place_cd
            }

            var repeat_type = element.repeat_type

            if (repeat_type == 0) { // none

                if (element.start_datetime !== undefined) {
                    eventsData.push({
                        'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                        'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                        'start': moment(element.start_datetime, 'YYYY.MM.DD HH:mm').format('YYYY-MM-DD HH:mm'),
                        'end': moment(element.end_datetime, 'YYYY.MM.DD HH:mm').format('YYYY-MM-DD HH:mm'),
                        'className': ["cell-color"], 'is_private': element.is_private,
                        'place_cd': element.place_cd
                    })
                }

            } else if (repeat_type == 1 || repeat_type == 2 || repeat_type == 3) { // daily | daily no holiday | weekly
                var from = null
                var to = null
                if (element.repeat_date_from != null && element.repeat_date_to != null) {
                    from = moment(new Date(element.repeat_date_from))
                    to = moment(new Date(element.repeat_date_to)).add(1, 'days')
                }
                var data = {
                    'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                    'typename': element.typename, 'typecolor': element.typecolor, 'color': element.color,
                    'startTime': element.time_from,
                    'endTime': element.time_to,
                    'className': ["cell-color"], 'is_private': element.is_private,
                    'place_cd': element.place_cd
                }
                if (from != null) {
                    data.startRecur = from.format('YYYY-MM-DD')
                }
                if (to != null) {
                    data.endRecur = to.format('YYYY-MM-DD')
                }

                if (repeat_type == 2) {
                    data.daysOfWeek = ['1', '2', '3', '4', '5']
                } else if (repeat_type == 3) {
                    data.daysOfWeek = [element.every_on]
                }

                eventsData.push(data)

            } else if (repeat_type == 4) { // monthly

                var from = null
                var to = null
                if (element.repeat_date_from != null && element.repeat_date_to != null) {
                    from = moment(new Date(element.repeat_date_from))
                    to = moment(new Date(element.repeat_date_to)).add(1, 'days')
                }
                var data = {
                    'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                    'typename': element.typename, 'typecolor': element.typecolor, 'color': element.color,
                    'startTime': element.time_from,
                    'endTime': element.time_to,
                    'className': ["cell-color"], 'is_private': element.is_private,
                    'place_cd': element.place_cd
                }
                if (from != null) {
                    data.startRecur = from.format('YYYY-MM-DD')
                }
                if (to != null) {
                    data.endRecur = to.format('YYYY-MM-DD')
                }

                data.daysOfMonth = [element.every_on]

                eventsData.push(data)
            }
        })
    }
}