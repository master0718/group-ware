function loadCalendarEventsData(user_id, resourceType, scheduleList, eventsData) {
    load_(user_id, resourceType, scheduleList, eventsData, 'staf_cd')
}

function load_(user_id, resourceType, list, eventsData, cdName) {
    if (list != null &&　list.length > 0) {
        list.forEach((element, index) => {

            var resourceId;
            if (resourceType == 'staf') {
                if (user_id == 0) resourceId = element[cdName]
                else resourceId = element[cdName] == user_id ? "1-" + user_id : "2-" + element[cdName]
            } else {
                resourceId = element.place_cd
            }

            if (element.repeat_type == 0) { // none

                if (element.start_datetime !== undefined) {
                    eventsData.push({
                        'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                        'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                        'start': moment(element.start_datetime, 'YYYY.MM.DD HH:mm').format('YYYY-MM-DD HH:mm'),
                        'end': moment(element.end_datetime, 'YYYY.MM.DD HH:mm').format('YYYY-MM-DD HH:mm'),
                        'className': ["cell-color"], 'is_private': element.is_private
                    })
                }

            } else if (element.repeat_type == 1) { // daily

                var diffDays = 0
                var from = null
                if (element.repeat_date_from != null && element.repeat_date_to != null) {
                    from = moment(new Date(element.repeat_date_from))
                    var to = moment(new Date(element.repeat_date_to))
                    diffDays = to.diff(from, "days")
                } else {
                    from = moment(new Date())
                    diffDays = 365
                }
                for (var i = 0; i <= diffDays; i++) {
                    var date = from.format('YYYY-MM-DD')
                    var time_from = element.time_from
                    var time_to = element.time_to
                    eventsData.push({
                        'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                        'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                        'start': date + ' ' + time_from,
                        'end': date + ' ' + time_to,
                        'className': ["cell-color"], 'is_private': element.is_private
                    })
                    from.add(1, 'days')
                }

            } else if (element.repeat_type == 2) { // daily no holiday

                var diffDays = 0
                var from = null
                if (element.repeat_date_from != null && element.repeat_date_to != null) {
                    from = moment(new Date(element.repeat_date_from))
                    var to = moment(new Date(element.repeat_date_to))
                    diffDays = to.diff(from, "days")
                } else {
                    from = moment(new Date())
                    diffDays = 365
                }
                for (var i = 0; i <= diffDays; i++) {
                    var weekday = from.isoWeekday()
                    if (weekday < 6 && !isHoliday(from)) {
                        var date = from.format('YYYY-MM-DD')
                        var time_from = element.time_from
                        var time_to = element.time_to
                        eventsData.push({
                            'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                            'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                            'start': date + ' ' + time_from,
                            'end': date + ' ' + time_to,
                            'className': ["cell-color"], 'is_private': element.is_private
                        })
                    }
                    from.add(1, 'days')
                }

            } else if (element.repeat_type == 3) { // weekly

                var weeks = 0
                var from = null
                if (element.repeat_date_from != null && element.repeat_date_to != null) {
                    from = moment(new Date(element.repeat_date_from))
                    var to = moment(new Date(element.repeat_date_to))
                    var diffDays = to.diff(from, "days")
                    weeks = Math.floor((diffDays + 7) / 7)
                } else {
                    from = moment(new Date())
                    weeks = 27
                }
                var every_on = element.every_on
                from = from.startOf('week').add(every_on, 'days')
                for (var i = 0; i <= weeks; i++) {
                    var date = from.format('YYYY-MM-DD')
                    var time_from = element.time_from
                    var time_to = element.time_to
                    eventsData.push({
                        'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                        'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                        'start': date + ' ' + time_from,
                        'end': date + ' ' + time_to,
                        'className': ["cell-color"], 'is_private': element.is_private
                    })
                    from.add(1, 'weeks')
                }

            } else if (element.repeat_type == 4) { // monthly

                var every_on = element.every_on
                var from = null
                if (element.repeat_date_from != null && element.repeat_date_to != null) {
                    from = moment(new Date(element.repeat_date_from))
                    var to = moment(new Date(element.repeat_date_to))
                    for (; ;) {
                        var startOf = moment(from.startOf('month'))
                        var from_ = startOf.add(every_on - 1, 'days')
                        if (from_.get('date') == every_on) {
                            var date = from_.format('YYYY-MM-DD')
                            var time_from = element.time_from
                            var time_to = element.time_to
                            eventsData.push({
                                'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                                'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                                'start': date + ' ' + time_from,
                                'end': date + ' ' + time_to,
                                'className': ["cell-color"], 'is_private': element.is_private
                            })
                        }
                        from.add(1, 'months')
                        if (from.isAfter(to))
                            break
                    }
                } else {
                    from = moment(new Date())
                    for (var i = 0; i < 12; i++) {
                        var startOf = moment(from.startOf('month'))
                        var from_ = startOf.add(every_on - 1, 'days')
                        if (from_.get('date') == every_on) {
                            var date = from_.format('YYYY-MM-DD')
                            var time_from = element.time_from
                            var time_to = element.time_to
                            eventsData.push({
                                'resourceId': resourceId, 'schedule_no': element.schedule_no, 'title': element.title,
                                'typename': element.typename, 'typecolor': element.typecolor, 'bkcolor': element.color,
                                'start': date + ' ' + time_from,
                                'end': date + ' ' + time_to,
                                'className': ["cell-color"], 'is_private': element.is_private
                            })
                        }
                        from.add(1, 'months')
                    }
                }
            }
        })
    }
}