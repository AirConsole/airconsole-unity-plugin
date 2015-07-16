 var airconsole;
/**
 * Sets up the communication to the screen.
 */
function App() {

    var me = this;

    me.airconsole = new AirConsole({"synchronize_time": true});
    me.airconsole.onMessage = function(from, data) {
      me.postToUnity({"action": "onMessage",
                      "from": from,
                      "data": data});
    };
    me.airconsole.onReady = function(code, device_id, device_data) {
      me.postToUnity({"action": "onReady",
                      "code": code,
                      "device_id": device_id,
                      "device_data": device_data,
                      "server_time_offset": me.airconsole.server_time_offset});

      // send inital device infos
      for (i = 0; i < me.airconsole.devices.length; i++) {
          me.airconsole.onDeviceStateChange(i, me.airconsole.devices[i]);
      }

    };

    me.airconsole.onDeviceStateChange = function(device_id, device_data) {
        me.postToUnity({"action": "onDeviceStateChange",
                        "device_id": device_id,
                        "device_data": device_data});
    };

};


App.prototype.postToUnity = function(data) {
    // send data with SendMessage from Unity js library
    SendMessage("AirController", "ProcessJS", JSON.stringify(data));
};

App.prototype.processUnityData = function (data) {
    var data = JSON.parse(data);

    if (data.action == "message") {
        this.airconsole.message(data.from, data.data);
    } else if (data.action == "broadcast") {
        this.airconsole.broadcast(data.data);
    } else if (data.action == "setCustomDeviceState") {
        this.airconsole.setCustomDeviceState(data.data);
        //this.airconsole.onDeviceStateChange(0, data.data);
    } else if (data.action == "debug") {
        console.log("debug message:", data.data);
    }
};

function onGameReady() {
    window.app = new App();
}