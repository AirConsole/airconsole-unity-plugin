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
            _webViewObject.SetMargins(0,0,0,0);
            
            _defaultScreenHeight = defaultScreenHeight;
        }

        internal void SetWebViewHeight(int webViewHeight) {
            _webViewHeight = webViewHeight;
        }

        internal void ActivateSafeArea() {
            Debug.Log("WebViewManager.ActivateSafeArea()");
#if WEBVIEWMANAGER_ACTIVE
            _isSafeAreaActive = true;
            _currentState = WebViewState.SafeAreaBased;

            UpdateViewView();
#endif
        }

        internal void RequestStateTransition(WebViewState newState) {
            Debug.Log($"WebViewManager.RequestStateTransition({newState})");
#if WEBVIEWMANAGER_ACTIVE
            // When the SafeArea has been activated, we do not allow any other state transitions anymore.
            if (_isSafeAreaActive) {
                Debug.Log($"WebViewManager.RequestStateTransition({newState}) => {_currentState}");
                return;
            }

            _currentState = newState;
            Debug.Log($"WebViewManager.RequestStateTransition({newState}) => {_currentState}");
            UpdateViewView();
#endif
        }

        public void UpdateViewView() {
            Debug.Log("WebViewManager.UpdateViewView()");
#if WEBVIEWMANAGER_ACTIVE
            switch (_currentState) {
                case WebViewState.Hidden:
                    _webViewObject.SetMargins(0, 0, 0, _defaultScreenHeight);
                    break;
                case WebViewState.TopBar:
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
#endif
        }
    }
}

#endif