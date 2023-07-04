/*
 * Copyright 2023 SanteSuite Inc. MAUI
 */
if (typeof __SanteDBAppService === 'undefined') {
    console.log("reinstantiating __SanteDBAppService");
    window.__SanteDBAppService = {};
}

const __sdbthis = this;
let __firstrun = true;

__SanteDBAppService.GetStatus = async function () {
    let statestr = __sdb_bridge.GetServiceState();
    console.log("Received state string");
    console.log(statestr);
    let state = JSON.parse(statestr);
    console.log(state);
    return state;
};

//(function updateState() {
//    setTimeout(async () => {
//        __SanteDBAppService._state = await __SanteDBAppService.GetStatus();

//        updateState();
//    }, __firstrun ? 1 : 1000);
//    __firstrun = false;
//})()

__SanteDBAppService.GetVersion = function () {
    return __sdb_bridge.GetVersion();
};

__SanteDBAppService.Print = function () {
    window.print();
};

__SanteDBAppService.Close = function () {
    window.console.log("Close function called.");
};

__SanteDBAppService.GetDataAsset = function (dataId) {

    return null;
};

if (window && window.crypto && window.crypto.randomUUID) {
    __SanteDBAppService.NewGuid = (function () {
        return this.crypto.randomUUID();
    }).bind(__sdbthis);
}
else {
    __SanteDBAppService.NewGuid = function () {
        let chars = "0123456789abcdef";
        let arr = [];

        for (let i = 0; i < 32; i++) {
            arr.push(chars[Math.random() * chars.length]);
        }
        arr.splice(20, 0, '-')
        arr.splice(16, 0, '-')
        arr.splice(12, 0, '-')
        arr.splice(8, 0, '-')

        return arr.join('');
    };
}

__SanteDBAppService.GetOnlineState = function () {
    return __sdb_bridge.GetOnlineState();
};

__SanteDBAppService.IsAdminAvailable = function () {
    return __sdb_bridge.IsAdminAvailable();
};

__SanteDBAppService.IsClinicalAvailable = function () {
    return __sdb_bridge.IsClinicalAvailable();
};

__SanteDBAppService.BarcodeScan = function () {
    return null;
};

__SanteDBAppService.ShowToast = function (text) {

    //const toastdata = {
    //    "version": 1,
    //    "title": null,
    //    "text": text,
    //    "icon": null
    //};

    //return window.fetch("_appservice/toast", {
    //    method: "POST",
    //    mode: "cors",
    //    cache: "no-cache",
    //    credentials: "same-origin",
    //    headers: {
    //        "Content-Type": "application/json"
    //    },
    //    redirect: "follow",
    //    referrerPolicy: "no-referrer",
    //    body: JSON.stringify(toastdata)
    //});
};

__SanteDBAppService.GetClientId = function () {
    return __sdb_bridge.GetClientId();
};

__SanteDBAppService.GetDeviceId = function () {
    return __sdb_bridge.GetDeviceId();
}

__SanteDBAppService.GetRealm = function () {
    return __sdb_bridge.GetRealm();
};

__SanteDBAppService.GetLocale = function () {
    if (window.sessionStorage.lang)
        return window.sessionStorage.lang;
    else
        return (navigator.language || navigator.userLanguage).substring(0, 2);
};

__SanteDBAppService.SetLocale = function (locale) {
    window.sessionStorage.lang = locale;
};

__SanteDBAppService.GetString = function (stringId) {
    return __sdb_bridge.GetString(stringId);
};

__SanteDBAppService.GetMagic = function () {
    return __sdb_bridge.GetMagic();
};

