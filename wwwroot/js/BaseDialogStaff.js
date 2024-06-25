$(function () {
    //var arr_select = $('.BaseDialogStaff_js_select');
    //console.log(arr_select.length);
    //for (var i = 0; i < arr_select.length; i++) {
    //    //console.log(arr_select[i]);
    //    $(arr_select[i]).css('width', '0px');
    //    $(arr_select[i]).css('height', '0px');
    //    $(arr_select[i]).css('min-height', '0px');
    //    $(arr_select[i]).css('padding', '0');
    //    var arr_options = $(arr_select[i]).find('option');
    //    for (var o = 0; o < arr_options.length; o++) {
    //        $(arr_options[o]).css('width', '0px');
    //        $(arr_options[o]).css('height', '0px');
    //        $(arr_options[o]).css('min-height', '0px');
    //        $(arr_options[o]).css('padding', '0');
    //    }
    //}

    $('.BaseDialogStaff_js_search').on('click', function () {

        $('#BaseDialogStaff_js_select_no').remove();
        var index_search = $('.BaseDialogStaff_js_search').index(this);
        $(this).append('<div id="BaseDialogStaff_js_select_no" data-BaseDialogStaff_js_select_no="' + index_search + '"></div>');
    });

    $(".BaseDialogStaff_js_choose").on('click', function () {

        parent.$.fn.colorbox.close();
        //①　親画面のセレクトボックスで選択されている社員の処理
        var dom_select = parent.$('.BaseDialogStaff_js_select')[parent.$('#BaseDialogStaff_js_select_no').attr('data-BaseDialogStaff_js_select_no')];
        var arr_dom_selected = $(dom_select).children(':selected');
        var arr_staff_select_list = new Array();
        var arr_staf_cd = new Array();
        for (var ss = 0; ss < arr_dom_selected.length; ss++) {
            var code = $(arr_dom_selected[ss]).val();
            arr_staf_cd.push(code);

            var staff = {
                staf_cd: code,
                staf_name: $(arr_dom_selected[ss]).text()
            };
            arr_staff_select_list.push(staff);
        }
        console.log("親画面のチェック済み追加");
        console.log(arr_staf_cd);
        console.log(arr_staff_select_list);
        //②　ダイアログで選択した社員の処理
        var arr_staff = new Array();

        var checked = $('td').find('input:checked');
        for (var i = 0; i < checked.length; i++) {
            var code = $($(checked[i]).closest('td').find('.staf_cd')[0]).val()
            var staff = {
                staf_cd: code,
                staf_name: $(checked[i]).closest('.staf_name').text()
            };
            arr_staff.push(staff);
        }
        console.log("ダイアログで選択されたもの");
        console.log(arr_staff);

        //③　②をループして①と重複していなければ追加
        for (var k = 0; k < arr_staff.length; k++) {
            var code = arr_staff[k]['staf_cd'];

            var staff = {
                staf_cd: code,
                staf_name: arr_staff[k]['staf_name']
            };
            if (arr_staff_select_list.includes(code)) {
                arr_staff_select_list = arr_staff_select_list.filter(r => r.staf_cd != staff.staf_cd);
            } else {
                arr_staf_cd.push(code);
            }
            arr_staff_select_list.push(staff);
        }
        console.log("親画面のチェック済み　更新");
        console.log(arr_staf_cd);
        console.log(arr_staff_select_list);
        //④　親画面セレクトボックスに反映
        $(dom_select).val(arr_staf_cd);

        Update_BaseDialogStaff_js_area_options(false);
        return false;
    });

    Update_BaseDialogStaff_js_area_options(true);
});

function Update_BaseDialogStaff_js_area_options(is_event_on_parent) {
    var arr_BaseDialogStaff_js_select = null;
    if (is_event_on_parent) {
        $('.BaseDialogStaff_js_area_options').remove();
        arr_BaseDialogStaff_js_select = $('.BaseDialogStaff_js_select');

    } else {
        parent.$('.BaseDialogStaff_js_area_options').remove();
        arr_BaseDialogStaff_js_select = parent.$('.BaseDialogStaff_js_select');
    }
    for (var s = 0; s < arr_BaseDialogStaff_js_select.length; s++) {
        var dom_select = arr_BaseDialogStaff_js_select[s];
        $(dom_select).after('<div class="BaseDialogStaff_js_area_options"></div>');
        var arr_dom_selected = $(dom_select).children(':selected');
        for (var ss = 0; ss < arr_dom_selected.length; ss++) {
            $(dom_select).next().append('<div class="pe-2" data-value="' + $(arr_dom_selected[ss]).val() + '">' + $(arr_dom_selected[ss]).text() + '<a href="#" class= "BaseDialogStaff_js_remove d-inline-block" title = "削除" >×</a ></div>');
        }
    }
    if (is_event_on_parent) {
        $('.BaseDialogStaff_js_remove').on("click", function (e) {
            console.log("削除します");
            e.preventDefault();
            var dom_select = $(this).closest('.BaseDialogStaff_js_area_options').prev().get();
            var arr_dom_selected = $(dom_select).children(':selected');
            var arr_updated_selected_value = new Array();

            for (var ss = 0; ss < arr_dom_selected.length; ss++) {
                if ($(arr_dom_selected[ss]).val() != $(this).closest('div').attr('data-value')) {
                    arr_updated_selected_value.push($(arr_dom_selected[ss]).val());
                }
            }
            $(dom_select).val(arr_updated_selected_value);
            $(this).closest('div').remove();
        });

    } else {
        //if (parent.$('.field-validation-error').length != 0) {
        //    parent.$(dom_select).closest('form').valid();
        //}

        parent.$('.BaseDialogStaff_js_remove').on("click", function (e) {
            console.log("削除します");
            e.preventDefault();
            var dom_select = $(this).closest('.BaseDialogStaff_js_area_options').prev().get();
            var arr_dom_selected = $(dom_select).children(':selected');
            var arr_updated_selected_value = new Array();

            for (var ss = 0; ss < arr_dom_selected.length; ss++) {
                if ($(arr_dom_selected[ss]).val() != $(this).closest('div').attr('data-value')) {
                    arr_updated_selected_value.push($(arr_dom_selected[ss]).val());
                }
            }
            $(dom_select).val(arr_updated_selected_value);
            //$(dom_select).closest('form').validate();

            //$(dom_select).closest('form').valid();

            $(this).closest('div').remove();

        //    //if ($(this).closest('form').find('.field-validation-error').length != 0) {
            //        console.log('valid');
        //        //$(this).closest('div').text($(this).closest('form').remove());
        //        //$('#test').val('dadffd');
        //    //$(this).closest('.BaseDialogStaff_js_select').change();
        //    $(dom_select).change();
        //        //$('#test').text($(this).closest('form').valid());

        //    //}

        });

    }

}
