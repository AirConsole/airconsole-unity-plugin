/**
 * Check if plugin is called from Unity-Editor
 */

var isEditor = false;
var wsPort;

if (window.location.pathname.split('/')[1].indexOf("port") > -1) {
    wsPort = window.location.pathname.split('/')[1].replace('port', '');
    isEditor = true;

    window.onload = function () {
        window.app = new App();
    };
}

/**
 * Sets up the communication to the screen.
 */

var airconsole;

function App() {

    var me = this;

    me.initEvents = function () {
        me.airconsole = new AirConsole({ "synchronize_time": true });
        me.airconsole.onMessage = function (from, data) {
            me.postToUnity({
                "action": "onMessage",
                "from": from,
                "data": data
            });
        };
        me.airconsole.onReady = function (code, device_id) {
            me.postToUnity({
                "action": "onReady",
                "code": code,
                "device_id": device_id,
                "devices": me.airconsole.devices,
                "server_time_offset": me.airconsole.server_time_offset
            });
        };
        me.airconsole.onDeviceStateChange = function (device_id, device_data) {
            me.postToUnity({
                "action": "onDeviceStateChange",
                "device_id": device_id,
                "device_data": device_data
            });
        };
    }

    if (isEditor) {
        me.setupConnection = function (reconnect) {

            me.unity_socket = new WebSocket("ws://127.0.0.1:" + wsPort + "/api");

            me.unity_socket.onopen = function (reconnect) {

                if (me.airconsole == null) {
                    me.initEvents();
                } else {
                    me.airconsole.onReady(); // resend onReady event
                }
            };

            me.unity_socket.onmessage = function (event) {
                me.processUnityData(event.data);
            };

            me.unity_socket.onclose = function () {
                console.log('lost connection to unity');
                window.setTimeout(function () {
                    console.log('try to reconnect to unity');
                    me.setupConnection(true);
                }, 5000);
            };
        };

        me.setupConnection();

    } else {
        me.initEvents();
    }
};

App.prototype.postToUnity = function (data) {

    if (isEditor) {
        // send data over websocket
        this.unity_socket.send(JSON.stringify(data));
    } else {
        // send data with SendMessage from Unity js library
        SendMessage("AirConsole", "ProcessJS", JSON.stringify(data));
    }

};

App.prototype.processUnityData = function (data) {
    var data = JSON.parse(data);

    if (data.action == "message") {
        this.airconsole.message(data.from, data.data);
    } else if (data.action == "broadcast") {
        this.airconsole.broadcast(data.data);
    } else if (data.action == "setCustomDeviceState") {
        this.airconsole.setCustomDeviceState(data.data);
    } else if(data.action == "showDefaultUI"){
        this.airconsole.showDefaultUI(data.data);
    } else if (data.action == "navigateHome") {
        this.airconsole.navigateHome();
    } else if (data.action == "loadScript") {
        this.airconsole.loadScript(data.data);
    } else if (data.action == "debug") {
        console.log("debug message:", data.data);
    }
};

function onGameReady(autoScale) {
    window.app = new App();

    if (autoScale) {
        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);
    }
}

function resizeCanvas() {
    var unityCanvas = document.getElementById('canvas');
    var aspectRatio = unityCanvas.width / unityCanvas.height;
    document.body.style.height = '100%';
    document.body.style.width = '100%';
    document.body.style.margin = '0px';
    unityCanvas.style.width = 100 + 'vw';
    unityCanvas.style.height = (100 / aspectRatio) + 'vw';
    unityCanvas.style.maxWidth = 100 * aspectRatio + 'vh';
    unityCanvas.style.maxHeight = 100 + 'vh';
    unityCanvas.style.margin = 'auto';
    unityCanvas.style.top = '0';
    unityCanvas.style.bottom = '0';
    unityCanvas.style.left = '0';
    unityCanvas.style.right = '0';
}