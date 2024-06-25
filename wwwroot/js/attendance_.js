

    var currentDate = new Date();
    var Year = currentDate.getFullYear();
    var Days;
    function save_year(e) {
        Year = e.target.value;
    }

    function show_days(e) {
        var month = e.target.value;
        month--;

        var nextMonth = new Date(Year, month + 1, 1);

        var lastDay = new Date(nextMonth - 1);

        Days = lastDay.getDate();
        document.getElementById("days").value = Days;

        $('#show_days_tbody').empty();
        for (var i = 0; i < Days; i++) {
            var tr = document.createElement('tr');

            // Create first cell (td) and its label
            var td1 = document.createElement('td');
            var label = document.createElement('label');
            label.setAttribute('for', 'inputName');
            label.textContent = (month + 1) + '月/' + (i + 1) + '日';
            td1.appendChild(label);

            // Create second cell (td) and its select box
            var td2 = document.createElement('td');
            var select = document.createElement('select');
            select.setAttribute('class', 'form-control');
            select.setAttribute('name', 'state');

            // Option values and texts
            var options = [
                { value: '1', text: '' },
                { value: '2', text: '有休' },
                { value: '3', text: '遅刻' },
                { value: '4', text: '早退' },
                { value: '5', text: '夏季休暇' }
            ];

            // Create and append options to the select box
            options.forEach(function (opt) {
                var option = document.createElement('option');
                option.value = opt.value;
                option.textContent = opt.text;
                select.appendChild(option);
            });

            // Append the select box to the second cell
            td2.appendChild(select);

            // Append both cells to the table row
            tr.appendChild(td1);
            tr.appendChild(td2);

            // Append the <tr> element to the <tbody> element
            document.getElementById('show_days_tbody').appendChild(tr);
        }
        $('#Sharemodel').modal('show');
    }

    function model_init(e) {
        var staff_name = e.target.closest("tr").querySelectorAll("td")[2].textContent;
        var staff_num = e.target.closest("tr").querySelectorAll("td")[1].textContent;
        console.log(staff_name);
        document.getElementById("year").value = Year;
        document.getElementById("staff_name").value = staff_name;
        document.getElementById("staff_num").value = staff_num;
    }

