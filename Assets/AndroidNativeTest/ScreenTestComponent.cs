using System.Linq;
using System.Text;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ScreenTestComponent : MonoBehaviour {
    [SerializeField]
    private TMPro.TMP_Text _textField;

    private Resolution _currentResolution;

    void Awake() {
        AirConsole.instance.onMessage += OnMessage;
        _currentResolution = Screen.currentResolution;
        _textField.text = GetScreenDataText();

        Screen.fullScreen = true;
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        _fullscreenMode = Screen.fullScreenMode;
    }

    private void OnDestroy() {
        if (AirConsole.instance != null) {
            AirConsole.instance.onMessage -= OnMessage;
        } 
    }

    // Update is called once per frame
    void Update()
    {
        _textField.text = GetScreenDataText();

        if (Input.touchCount > 0 && Input.touches.Any(t => t.phase == TouchPhase.Ended)) {
            Screen.fullScreen = !Screen.fullScreen;
        }
    }

    string GetScreenDataText() {
        StringBuilder sb = new();
        sb.AppendLine(
            $"Initial Screen Configuration: {_currentResolution.width}x{_currentResolution.height}@{_currentResolution.refreshRate:N0}Hz");
        sb.AppendLine($"Current Screen Configuration: {Screen.currentResolution.width}x{Screen.currentResolution.height}@{Screen.currentResolution.refreshRate:N0}Hz");
        
        // UnityEngine.Device.Screen.GetDisplayLayout(list);
        // list.ForEach(di => sb.AppendLine($"DI {di.name}: {di.width}x{di.height}@{di.refreshRate.value:N0}Hz; Area {di.workArea}"));
        int index = 0;
        foreach (Display display in Display.displays) {
            sb.AppendLine(
                $"DI {index++}: {display.systemWidth}x{display.systemHeight} -> {display.renderingWidth}x{display.renderingHeight}");
        } 

        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"Screen Height: {Screen.height}");
        sb.AppendLine($"Main Window Position: {Screen.mainWindowPosition}");
        sb.AppendLine($"Android Resize Mode: {AirConsole.instance.androidUIResizeMode}");
        sb.AppendLine($"Fullscreen: {Screen.fullScreen} @ {Screen.fullScreenMode}");
        for (var i = 0; i < Screen.cutouts.Length; i++) {
            sb.AppendLine($"Cutout {i}: {Screen.cutouts[i]}");
        }

        sb.AppendLine($"SafeArea: {Screen.safeArea}");
        sb.AppendLine($"AC SafeArea: {AirConsole.instance.SafeArea}");
        sb.AppendLine($"MainCam Pixel: {Camera.main.pixelRect}");
        return sb.ToString();
    }

    private FullScreenMode _fullscreenMode;
    void OnMessage(int from, JToken data) {
        switch ((string)data["action"]) {
            case "fullscreen": {
                
                Screen.fullScreen = (bool)data["value"];
                break;
            }

            case "fullscreenMode": {
                _fullscreenMode = (FullScreenMode)(((int)_fullscreenMode + 1) % 3);
                Screen.fullScreenMode = _fullscreenMode;
                break;
            }

            case "androidUIResizeMode": {
                
                AirConsole.instance.androidUIResizeMode = (AndroidUIResizeMode)(((int)AirConsole.instance.androidUIResizeMode + 1) % 3);
                break;
            }
        }
    }
}
