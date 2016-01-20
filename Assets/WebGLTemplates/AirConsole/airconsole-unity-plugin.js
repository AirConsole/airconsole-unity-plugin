/**
 * Copyright by N-Dream AG 2016.
 * @version 1.3.0.c
 */

/**
 * Check if plugin is called from Unity-Editor
 */

var isEditor = false;
var isUnityReady = false;

function getURLParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

var wsPort = getURLParameterByName("unity-editor-websocket-port");
if (wsPort) {
    isEditor = true;
}

/**
 * Sets up the communication to the screen.
 */

function App() {

    var me = this;
    me.queue = false;

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
            me.postToUnity({
                "action": "onReady",
                "code": code,
                "device_id": me.airconsole.device_id,
                "devices": me.airconsole.devices,
                "server_time_offset": me.airconsole.server_time_offset,
                "location": document.location.href
            });
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
        
        me.airconsole.onDeviceProfileChange = function(device_id) {
            me.postToUnity({
                "action": "onDeviceProfileChange",
                "device_id": device_id
            });
        }
    }

    if (isEditor) {
        me.setupConnection = function () {

            me.unity_socket = new WebSocket("ws://127.0.0.1:" + wsPort + "/api");

            me.unity_socket.onopen = function () {
                isUnityReady = true;
                if (me.airconsole == null) {
                    me.initEvents();
                } else {
                    me.postQueue();
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

App.prototype.postQueue = function () {
    for (var i = 0; i < this.queue.length; ++i) {
      this.postToUnity(this.queue[i]);
	}
	this.queue = false;
}

App.prototype.postToUnity = function (data) {
    if (isUnityReady) {
	    if (isEditor) {
	        // send data over websocket
	        this.unity_socket.send(JSON.stringify(data));

	    } else {
	        // send data with SendMessage from Unity js library
	        SendMessage("AirConsole", "ProcessJS", JSON.stringify(data));
	    }
	} else {
	    if (this.queue === false && data.action == "onReady") {
		  this.queue = [];
		}
		if (this.queue !== false) {
		  this.queue.push(data);
		}
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
    } else if (data.action == "setActivePlayers") {
        this.airconsole.setActivePlayers(data.max_players);
    } else if (data.action == "debug") {
        console.log("debug message:", data.data);
    }
};

function onGameReady(autoScale) {
    isUnityReady = true;

    // send cached onRadyData
    window.app.postQueue();

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
    document.body.style.overflow = 'hidden';
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
 
function initAirConsole() {
    window.app = new App();
	if (isEditor) {
        document.body.innerHTML = "<div style='position:absolute; top:50%; left:0%; width:100%; margin-top:-32px; color:white;'>"
            + "<div id='editor-message' style='text-align:center; font-family: Arial'><div style='font-size:32px;'>You can see the game scene in the Unity Editor.</div><br>Keep this window open in the background.</div>"
            + "</div>";
	}
}