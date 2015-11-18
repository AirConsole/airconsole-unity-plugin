/**
 * Check if plugin is called from Unity-Editor
 */

var isEditor = false;
var isUnityReady = false;
var wsPort;

if (window.location.pathname.split('/')[1].indexOf("port") > -1) {
    wsPort = window.location.pathname.split('/')[1].replace('port', '');
    isEditor = true;
}

/**
 * Sets up the communication to the screen.
 */

function App() {

    var me = this;
    me.onReadyData = null;

    me.initEvents = function () {
        me.airconsole = new AirConsole({ "synchronize_time": true });

        me.airconsole.onMessage = function (from, data) {
            me.postToUnity({
                "action": "onMessage",
                "from": from,
                "data": data
            });
        };

        me.airconsole.onReady = function (code) {
            me.onReadyData = {
                "action": "onReady",
                "code": code,
                "device_id": me.airconsole.device_id,
                "devices": me.airconsole.devices,
                "server_time_offset": me.airconsole.server_time_offset,
                "location": document.location.href
            };
            me.postToUnity(me.onReadyData);
        };

        me.airconsole.onDeviceStateChange = function (device_id, device_data) {
            me.postToUnity({
                "action": "onDeviceStateChange",
                "device_id": device_id,
                "device_data": device_data
            });
        };
        
        me.airconsole.onConnect = function (device_id) {
            me.postToUnity({
                "action": "onConnect",
                "device_id": device_id
            });
        };
        
        me.airconsole.onDisconnect = function (device_id) {
            me.postToUnity({
                "action": "onDisconnect",
                "device_id": device_id
            });
        };
        
        me.airconsole.onCustomDeviceStateChange = function (device_id) {
            me.postToUnity({
                "action": "onCustomDeviceStateChange",
                "device_id": device_id
            });
        };
    }

    if (isEditor) {
        me.setupConnection = function () {

            me.unity_socket = new WebSocket("ws://127.0.0.1:" + wsPort + "/api");

            me.unity_socket.onopen = function () {
                if (me.airconsole == null) {
                    me.initEvents();
                } else {
                    me.postToUnity(me.onReadyData); // resend onReady event
                }
            };

            me.unity_socket.onmessage = function (event) {
                me.processUnityData(event.data);
            };

            me.unity_socket.onclose = function () {
                document.getElementById("editor-message").innerHTML = "<span style='font-size:32px'>Game <span style='color:red'>stopped</span> in Unity. Please close this tab.</span></span>";
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

    } else if (isUnityReady) {
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
    } else if (data.action == "setCustomDeviceStateProperty") {
        this.airconsole.setCustomDeviceStateProperty(data.key, data.value);
    } else if (data.action == "showDefaultUI") {
        this.airconsole.showDefaultUI(data.data);
    } else if (data.action == "navigateHome") {
        this.airconsole.navigateHome();
    } else if (data.action == "navigateTo") {
        this.airconsole.navigateTo(data.data);
    } else if (data.action == "debug") {
        console.log("debug message:", data.data);
    }
};

function onGameReady(autoScale) {
    isUnityReady = true;

    // send cached onRadyData
    window.app.postToUnity(window.app.onReadyData);

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

/**
 * Run AirConsole
 */
window.app = new App();

if (isEditor) {
    window.onload = function () {
        document.body.innerHTML = "<div style='position:absolute; top:50%; left:50%; transform: translate(-50%, -50%); color:white;'>"
            + "<div id='editor-message' style='text-align:center; font-family: Arial'><div style='font-size:32px;'>You can see the game scene in the Unity Editor.</div><br>Keep this window open in the background.</div>"
            + "</div>";
    }
}