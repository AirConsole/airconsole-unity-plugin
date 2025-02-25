using System.Collections.Generic;
using Newtonsoft.Json;

namespace NDream.AirConsole.Android.Plugin {
    [JsonObject("CarInfo")]
    public struct CarInformation {
        [JsonProperty("authToken")]
        public Dictionary<string, string> AuthToken  { get; internal set; }
            
        [JsonProperty("complete")]
        public bool Complete  { get; internal set; }
           
        [JsonProperty("homeCountry")]
        public string HomeCountry  { get; internal set; }
           
        [JsonProperty("model")] 
        public string Model { get; internal set; }
            
        [JsonProperty("swPu")]
        public string SoftwareVersion { get; internal set; }

        public override string ToString() {
            return $"Software Version: {SoftwareVersion}, Model: {Model}, Home Country: {HomeCountry}, Complete: {Complete}, Auth Tokens: {AuthToken.Count}";
        }
    }
}