$(function () {
    let url = baseUrl;
    // #region ダウンロードボタン
    $(document).on("click", ".layout_download_file", function () {
        var dic_cd = $(this).data('dic_cd');
        var dir_no = $(this).data('dir_no');
        var file_name = $(this).data('file_name');
        let url = baseUrl + "Notice/DownloadFile?" + "dic_cd=" + dic_cd + "&dir_no=" + dir_no + "&file_name=" + file_name;

        //指定したURLからファイルをダウンロードする
        funcFileDownload(url, file_name);
    });
    // #endregion

    GetBukkenCommentReadCount(url);
    GetMemoReadCount(url);

    $.ajax({
        type: "POST",
        url: url + "Base/GetReportCount",
        success: function (ret) {
            $("#layout_report_count").text(ret);
        },
        error: function (e) {
            $("#layout_report_count").text("件数取得失敗");
        },
    });
    $.ajax({
        type: "POST",
        url: url + "Base/GetGroupItems",
        success: function (ret) {
            var totalCount = 0;
            var html = '<ul class="nav-second-level">';
            for (var i = 0; i < ret.length; i++) {
                var group = ret[i];
                html += '<li>';

                // Manually construct the URL with the group_cd parameter
                var url = `/groupware/EmployeeGroup/GetDetails/${group.group_cd}`;

                html += `<a href="${url}">${group.group_name}` + ` (${group.user_count})</a>`;
                html += '</li>';
                totalCount += group.user_count;
            }
            html += '</ul>';
            $('#total-count').text('(' + totalCount + ')');
            $('#menu-container').html(html);
        },
        error: function (e) {
            console.log('error here?', e);
        },
    });


    // 確認ボタンをクリックするとイベント発動
    $('.kakuninDialog').on('click', function (e) {
        var obj_form = $(this).closest('form');
        if ($(this).attr('data-site_form') != undefined) {
            obj_form = $('#' + $(this).attr('data-site_form'));
        }
        var message = "登録してもよろしいですか？";
        //文言変更
        if ($(this).attr('data-site_confirm') != undefined) {
            message = $(this).attr('data-site_confirm')
        }
        if (obj_form.valid()) {
            if (!confirm(message)) {
                // もしキャンセルをクリックしたら
                // submitボタンの効果をキャンセルし、クリックしても何も起きない
                return false;
            } else {
                // 「OK」をクリックした際の処理を記述
                obj_form.trigger("submit");
            }
        }
    });
    $('input').on('change', function (e) {
        $(this).closest('form').find(".validation-summary-errors").addClass('d-none');
    });
    $('.site_change_post').on('change', function () {
        $(this).closest('form').trigger("submit");
    });
    //datatable
    $(".datatables").DataTable({
        "language": {
            url: "https://cdn.datatables.net/plug-ins/1.11.5/i18n/ja.json",
        },
        //"columnDefs": [
        //    { "searchable": false, "targets": 1 },
        //    { "searchable": false, "targets": 2 }
        //],
        searching: false,
        "order": [],
        fixedHeader: true,//テーブルヘッダーを固定
        lengthChange: false,
        displayLength: 500,
        scrollY: "500px",
        scrollCollapse: true,//データ行数が少ない場合に調整する
    });
    $(".datatables-search").DataTable({
        "language": {
            url: "https://cdn.datatables.net/plug-ins/1.11.5/i18n/ja.json",
        },
        searching: true,
        "order": []
    });
    $(".RestrationReport_datatables").DataTable({
        "language": {
            url: "https://cdn.datatables.net/plug-ins/1.11.5/i18n/ja.json",
        },
        //"columnDefs": [
        //    { "searchable": false, "targets": 1 },
        //    { "searchable": false, "targets": 2 }
        //],
        searching: false,
        "order": [],
        fixedHeader: true,//テーブルヘッダーを固定
        lengthChange: false,
        displayLength: 500,
        scrollY: "500px",
        scrollCollapse: true,//データ行数が少ない場合に調整する
        scrollX: true,
    });

    $('.site_iframe').colorbox(
        {
            iframe: true,
            width: "80%", height: "80%",
            opacity: 0.5
        });
    $('.site_iframe_30_80').colorbox(
        {
            iframe: true,
            width: "30%", height: "80%",
            opacity: 0.5
        });

    $(".site_iframe_close").on('click', function () {
        parent.$.fn.colorbox.close();
        return false;
    });
    $('.site_calendar_icon').on('click', function () {
        $(this).prev('.site_calendar').focus();
    });

    $.ajax({
        type: "GET",
        url: baseUrl + "Base/GetHolidays",
        success: function (result) {
            holidays = result;

            if ($('.site_calendar').length) {
                $('.site_calendar').datepicker({
                    beforeShowDay: markHolidays,
                    language: 'ja',
                });
                initDate();
            }
        },
        error: function (xhr) {
        }
    });
});

function initDate() {
//    let fromDate = new Date($('#start_datetime').attr('value'));
//    $('#input-date').datepicker('setDate', fromDate);

//    $('#btnToday').on('click', function () {
//        var datetime = moment(new Date()).format("YYYY/MM/DD")
//        $("#input-date").val(datetime)
//    });
//    $('#date-from-icon').click(function() {
//        $("#input-date").focus();
//    });
}

function markHolidays(date) {
    if (date.getDay() === 0) {
        // 日曜日の場合
        return [true, "day-sunday", ""];
    } else if (date.getDay() === 6) {
        // 土曜日の場合
        return [true, "day-saturday", ""];
    }
    else {
        for (var i = 0; i < holidays.length; i++) {
            var stringDate = $.datepicker.formatDate('yy-mm-dd', date);

            if ($.inArray(stringDate, holidays) !== -1) {
                return [true, 'day-holiday', ''];
            } else {
                return [true, ''];
            }
        }
    }
}

function isHoliday(date) {
    var stringDate = date.format('yy-MM-DD');
    if ($.inArray(stringDate, holidays) !== -1) {
        return true;
    }
    else {
        return false;
    }
}

/**
 * 非同期通信　共通関数　PDFファイルのダウンロード
 *
 * */
function funcFileDownload(url, filename, messageForFailure) {
    //show_loading();
    var xhr = new XMLHttpRequest();
    xhr.open("GET", url, true);
    xhr.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
    xhr.responseType = "blob";
    xhr.onloadend = function () {
        //hide_loading();

    };

    xhr.onloadstart = function () {
        //show_loading();
    };

    xhr.onload = function () {
        console.log(this.status);
        if (xhr.readyState === 4 && xhr.status === 200) {
            var blob = new Blob([xhr.response]);
            const url = window.URL.createObjectURL(blob);
            var a = document.createElement('A');
            a.href = url;
            a.download = filename;
            a.click();
            setTimeout(function () {
                window.URL.revokeObjectURL(blob)
                    , 100
            });

            //メッセージ表示
            toastr.options = {
                "closeButton": true,
                "debug": false,
                "newestOnTop": false,
                "progressBar": false,
                "positionClass": "toast-bottom-left",
                "preventDuplicates": false,
                "onclick": null,
                "showDuration": "300",
                "hideDuration": "1000",
                "timeOut": "5000",
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut"
            }
            toastr["success"]("ダウンロードに成功しました");
        } else {
            toastr.options = {
                "closeButton": true,
                "debug": false,
                "newestOnTop": false,
                "progressBar": false,
                "positionClass": "toast-bottom-left",
                "preventDuplicates": false,
                "onclick": null,
                "showDuration": "300",
                "hideDuration": "1000",
                "timeOut": "5000",
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut"
            }
            toastr["error"](messageForFailure == null ? "ダウンロードに失敗しました" : messageForFailure);

        }

    };

    xhr.send();
}
function GetBukkenCommentReadCount(url) {
    $.ajax({
        type: "POST",
        url: url + "Base/GetBukkenCommentReadCount",
        success: function (ret) {
            $("#layout_bukken_memo_count").text(ret['count']);
        },
        error: function (e) {
            $("#layout_bukken_memo_count").text("件数取得失敗");
        },
    });
}

function GetMemoReadCount(url) {
    $.ajax({
        type: "POST",
        url: url + "Memo/GetMemoReadCount",
        success: function (ret) {
            var count = ret['count'];
            if (count > 0)
                $(".memo_unread_count").text(count);
        },
        error: function (e) {
        },
    });
}


// navigation
/**
* LeftSidebar
* @param {*} $
*/
!function ($) {
    'use strict';

    var LeftSidebar = function () {
        this.body = $('body'),
            this.window = $(window)
    };

    /**
     * Initilizes the menu
     */
    LeftSidebar.prototype.initMenu = function () {
        var self = this;

        // Left menu collapse
        $('.button-menu-mobile').on('click', function (event) {
            event.preventDefault();
            self.body.toggleClass('sidebar-enable');
        });

        // sidebar - main menu
        if ($("#side-menu").length) {
            var navCollapse = $('#side-menu li .collapse');
            var navToggle = $("#side-menu [data-bs-toggle='collapse']");
            navToggle.on('click', function (e) {
                return false;
            });
            // open one menu at a time only

            navCollapse.on({
                'show.bs.collapse': function (event) {
                    $('#side-menu .collapse.show').not(parent).collapse('hide');
                    var parent = $(event.target).parents('.collapse.show');
                },
            });


            // activate the menu in left side bar (Vertical Menu) based on url
            $("#side-menu a").each(function () {
                var pageUrl = window.location.href.split(/[?#]/)[0];
                if (this.href == pageUrl) {
                    $(this).addClass("active");
                    $(this).parent().addClass("menuitem-active");
                    $(this).parent().parent().parent().addClass("show");
                    $(this).parent().parent().parent().parent().addClass("menuitem-active"); // add active to li of the current link

                    var firstLevelParent = $(this).parent().parent().parent().parent().parent().parent();
                    if (firstLevelParent.attr('id') !== 'sidebar-menu')
                        firstLevelParent.addClass("show");

                    $(this).parent().parent().parent().parent().parent().parent().parent().addClass("menuitem-active");

                    var secondLevelParent = $(this).parent().parent().parent().parent().parent().parent().parent().parent().parent();
                    if (secondLevelParent.attr('id') !== 'wrapper')
                        secondLevelParent.addClass("show");

                    var upperLevelParent = $(this).parent().parent().parent().parent().parent().parent().parent().parent().parent().parent();
                    if (!upperLevelParent.is('body'))
                        upperLevelParent.addClass("menuitem-active");
                }
            });
        }

        // handling two columns menu if present
    },

        /**
         * Initilizes the menu
         */
        LeftSidebar.prototype.init = function () {
            this.initMenu();

            $(document).on('click', 'body', function (e) {
                if ($(e.target).closest('.left-side-menu, .side-nav').length > 0 || $(e.target).hasClass('button-menu-mobile')
                    || $(e.target).closest('.button-menu-mobile').length > 0) {
                    return;
                }

                $('body').removeClass('sidebar-enable');
                return;
            });
        },

        $.LeftSidebar = new LeftSidebar, $.LeftSidebar.Constructor = LeftSidebar
}(window.jQuery),


    /**
     * Topbar
     * @param {*} $
     */
    function ($) {
        'use strict';

        var Topbar = function () {
            this.body = $('body'),
                this.window = $(window)
        };

        /**
         * Initilizes the menu
         */
        Topbar.prototype.initMenu = function () {

            //activate the menu in topbar(horizontal menu) based on url
            $(".navbar-nav a").each(function () {
                var pageUrl = window.location.href.split(/[?#]/)[0];
                if (this.href == pageUrl) {
                    $(this).addClass("active");
                    $(this).parent().addClass("active");
                    $(this).parent().parent().addClass("active");
                    $(this).parent().parent().parent().addClass("active");
                    $(this).parent().parent().parent().parent().addClass("active");
                    var el = $(this).parent().parent().parent().parent().addClass("active").prev();
                    if (el.hasClass("nav-link"))
                        el.addClass('active');
                }
            });

            // Topbar - main menu
            $('.navbar-toggle').on('click', function (event) {
                $(this).toggleClass('open');
                $('#navigation').slideToggle(400);
            });
        },

            /**
             * Initilizes the menu
             */
            Topbar.prototype.init = function () {
                this.initMenu();
            },
            $.Topbar = new Topbar, $.Topbar.Constructor = Topbar
    }(window.jQuery),


    /**
     * Layout and theme manager
     * @param {*} $
     */

    function ($) {
        'use strict';

        // Layout and theme manager

        var LayoutThemeApp = function () {
            this.body = $('body'),
                this.window = $(window);
        };

        /**
         * Init
         */
        LayoutThemeApp.prototype.init = function () {
            this.leftSidebar = $.LeftSidebar;
            this.topbar = $.Topbar;

            this.leftSidebar.init();
            this.topbar.init();

            this.leftSidebar.parent = this;
            this.topbar.parent = this;
        },

            $.LayoutThemeApp = new LayoutThemeApp, $.LayoutThemeApp.Constructor = LayoutThemeApp
    }(window.jQuery);

!function ($) {
    'use strict';

    var App = function () {
        this.$body = $('body'),
            this.$window = $(window)
    };

    /**
     * Initlizes the controls
    */
    App.prototype.initControls = function () {
        // remove loading
        setTimeout(function () {
            document.body.classList.remove('loading');
        }, 400);

        // Preloader
        $(window).on('load', function () {
            $('#status').fadeOut();
            $('#preloader').delay(350).fadeOut('slow');
        });

        $('[data-toggle="fullscreen"]').on("click", function (e) {
            e.preventDefault();
            $('body').toggleClass('fullscreen-enable');
            if (!document.fullscreenElement && /* alternative standard method */ !document.mozFullScreenElement && !document.webkitFullscreenElement) {  // current working methods
                if (document.documentElement.requestFullscreen) {
                    document.documentElement.requestFullscreen();
                } else if (document.documentElement.mozRequestFullScreen) {
                    document.documentElement.mozRequestFullScreen();
                } else if (document.documentElement.webkitRequestFullscreen) {
                    document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
                }
            } else {
                if (document.cancelFullScreen) {
                    document.cancelFullScreen();
                } else if (document.mozCancelFullScreen) {
                    document.mozCancelFullScreen();
                } else if (document.webkitCancelFullScreen) {
                    document.webkitCancelFullScreen();
                }
            }
        });
        document.addEventListener('fullscreenchange', exitHandler);
        document.addEventListener("webkitfullscreenchange", exitHandler);
        document.addEventListener("mozfullscreenchange", exitHandler);
        function exitHandler() {
            if (!document.webkitIsFullScreen && !document.mozFullScreen && !document.msFullscreenElement) {
                console.log('pressed');
                $('body').removeClass('fullscreen-enable');
            }
        }
    },

        //initilizing
        App.prototype.init = function () {

            this.initControls();

            // init layout
            this.layout = $.LayoutThemeApp;

            this.layout.init();
        },

        $.App = new App, $.App.Constructor = App


}(window.jQuery),
    //initializing main application module
    function ($) {
        "use strict";
        $.App.init();
    }(window.jQuery);

//# sourceMappingURL=app.min.js.map
