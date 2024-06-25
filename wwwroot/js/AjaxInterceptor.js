/**
 * Ajax通信 Interceptor
 * 取り扱い注意
 */

// #region Ajax send interseptor
$(document).ajaxSend(function (event, jqxhr, settings) {
    settings.url = GetContextPathPrefix() + settings.url;
});
// #endregion

// #region Get Context Path Prefix
/**
 * Get Context path prefix
 */
function GetContextPathPrefix() {
    const pathName = window.location.pathname;

    // no match by default
    return pathName.substr(0, pathName.lastIndexOf('/'));
}
// #endregion

// #region contextPathのPrefix取り出し
function fetchPrefix(_url, _arg) {
    return _url.substr(0, _url.indexOf(_arg));
}
// #endregoin

// #region Get Context Path
/**
 * Get Context Path
 */
function GetContextPath() {
    const pathName = window.location.pathname;

    // no match by default
    return pathName.substr(0, pathName.lastIndexOf('/'));
}

/*
 * コンテキストパスの先頭を取得する
 * ただしコンテキストパスが１個の場合はコントローラと判断し空文字を返す
*/

function GetFirstContextPath() {
    const pathName = window.location.pathname;

    // no match by default
    var targetStr = "/";

    var count = (pathName.match(new RegExp(targetStr, "g")) || []).length;

    if (count >= 2) {
        return (pathName.substr(pathName.indexOf('/'), pathName.indexOf('/', pathName.indexOf('/') + 1)));
    } else {
        return "";
    }
}

/*
 * コンテキストパスの2番目までを取得する
 * ただしコンテキストパスが１個の場合はコントローラと判断し空文字を返す
*/

function GetTwoContextPath() {
    const pathName = window.location.pathname;

    // no match by default
    var targetStr = "/";

    var count = (pathName.match(new RegExp(targetStr, "g")) || []).length;

    if (count >= 2) {
        var array_pathName = pathName.split("/");
        return ("/"+array_pathName[1] +"/"+ array_pathName[2]);
    } else {
        return "";
    }
}


// #endregion

// #region contextPath取り出し
function fetchContextPath(_url, _arg) {
    return _url.substr(_url.indexOf(_arg), _arg.length);
}
// #endregoin
