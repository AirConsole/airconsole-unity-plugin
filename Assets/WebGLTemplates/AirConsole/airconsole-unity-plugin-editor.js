var airconsole;
/**
 * Sets up the communication to the screen.
 */
function App() {

    var me = this;
    
    me.setupConnection = function (reconnect){
        
        me.unity_socket = new WebSocket("ws://localhost:7843/api");

        me.unity_socket.onopen = function(reconnect) {

            if(me.airconsole == null){
                console.log('created airconsole');
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
            
            } else {
                // resend onReady event
                me.airconsole.onReady();
            }
        };

        me.unity_socket.onmessage = function(event) {
            me.processUnityData(event.data);
        };

        me.unity_socket.onclose = function() {
            console.log('lost connection to unity');
            window.setTimeout(function(){
                console.log('try to reconnect to unity');
                me.setupConnection(true);
            },3000);
        };
    };
    
    me.setupConnection();
}

App.prototype.postToUnity = function(data) {
    this.unity_socket.send(JSON.stringify(data));
};

App.prototype.processUnityData = function(data) {
    var data = JSON.parse(data);
    if (data.action == "message") {
        this.airconsole.message(data.from, data.data);
    } else if (data.action == "broadcast") {
        this.airconsole.broadcast(data.data);
    } else if (data.action == "setCustomDeviceState") {
        this.airconsole.setCustomDeviceState(data.data);
    } else if (data.action == "debug") {
        console.log("debug message:", data.data);
    }
};

window.onload = function(){
    window.app = new App();
};