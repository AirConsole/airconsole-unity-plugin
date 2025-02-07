#if !DISABLE_AIRCONSOLE
#if UNITY_ANDROID && !UNITY_EDITOR
#define WEBVIEWMANAGER_ACTIVE
#endif

using UnityEngine;

namespace NDream.AirConsole {
    public class WebViewManager {
        private WebViewObject _webViewObject;
        private WebViewState _currentState;
        private bool _isSafeAreaActive;
        private int _defaultScreenHeight;
        private int _webViewHeight;

        internal enum WebViewState {
            Hidden,
            TopBar,
            FullScreen,
            SafeAreaBased
        }

        internal WebViewManager(WebViewObject webViewObject, int defaultScreenHeight) {
            _webViewObject = webViewObject;
            _webViewObject.SetMargins(0,0,0, defaultScreenHeight);
            
            _defaultScreenHeight = defaultScreenHeight;
        }

        internal void SetWebViewHeight(int webViewHeight) {
            _webViewHeight = webViewHeight;
#if WEBVIEWMANAGER_ACTIVE
            UpdateWebView();
#endif
        }

        internal void ActivateSafeArea() {
            AirConsoleLogger.LogDevelopment("WebViewManager.ActivateSafeArea()");
#if WEBVIEWMANAGER_ACTIVE
            _isSafeAreaActive = true;
            _currentState = WebViewState.SafeAreaBased;
            UpdateWebView();
#endif
        }

        internal void RequestStateTransition(WebViewState newState) {
            AirConsoleLogger.LogDevelopment($"WebViewManager.RequestStateTransition({newState})");
#if WEBVIEWMANAGER_ACTIVE
            // When the SafeArea has been activated, we do not allow any other state transitions anymore.
            if (_isSafeAreaActive) {
                Debug.Log($"WebViewManager.RequestStateTransition({newState}) => {_currentState}");
                return;
            }

            _currentState = newState;
            Debug.Log($"WebViewManager.RequestStateTransition({newState}) => {_currentState}");
            UpdateWebView();
#endif
        }

        public void UpdateWebView() {
            AirConsoleLogger.LogDevelopment("WebViewManager.UpdateWebView()");
#if WEBVIEWMANAGER_ACTIVE
            switch (_currentState) {
                case WebViewState.Hidden:
                    _webViewObject.SetMargins(0, 0, 0, _defaultScreenHeight);
                    break;
                case WebViewState.TopBar:
                    AirConsoleLogger.LogDevelopment($"WebViewManager.UpdateWebView w/ Topbar: {_defaultScreenHeight}, {_webViewHeight} => bottomMargin: {(_defaultScreenHeight - _webViewHeight)}");
                    _webViewObject.SetMargins(0, 0, 0, _defaultScreenHeight - _webViewHeight);
                    break;
                case WebViewState.FullScreen:
                    _webViewObject.SetMargins(0, 0, 0, 0);
                    break;
                case WebViewState.SafeAreaBased:
                    _webViewObject.SetMargins(0, 0, 0, 0);
                    break;
            }

            _webViewObject.SetVisibility(_currentState != WebViewState.Hidden && !Application.isEditor);
            AirConsoleLogger.LogDevelopment($"WebViewManager.UpdateWebView: webview setVisibility: {(_currentState != WebViewState.Hidden && !Application.isEditor)}");
#endif
        }
    }
}

#endif