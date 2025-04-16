#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole {
    using UnityEngine;
    using System;

    public class WebViewManager {
        private readonly WebViewObject _webViewObject;
        private readonly int _defaultScreenHeight;
        
        private WebViewState _currentState;
        private bool _isSafeAreaActive;
        private int _webViewHeight;

        internal enum WebViewState {
            Hidden,
            TopBar,
            FullScreen,
            SafeAreaBased
        }

        internal WebViewManager(WebViewObject webViewObject, int defaultScreenHeight) {
            _webViewObject = webViewObject;
            _webViewObject.SetMargins(0, 0, 0, 0);

            _defaultScreenHeight = defaultScreenHeight;
        }

        internal void SetWebViewHeight(int webViewHeight) {
            _webViewHeight = webViewHeight;
            UpdateWebView();
        }

        internal void ActivateSafeArea() {
            AirConsoleLogger.LogDevelopment("WebViewManager.ActivateSafeArea()");
            _isSafeAreaActive = true;
            _currentState = WebViewState.SafeAreaBased;
            UpdateWebView();
        }

        internal void RequestStateTransition(WebViewState newState) {
            Debug.Log($"WebViewManager.RequestStateTransition: {_currentState} => {newState}");
            // When the SafeArea has been activated, we do not allow any other state transitions anymore.
            // The only thing allowed after this is for the safe area itself to change.
            if (_isSafeAreaActive) {
                return;
            }

            _currentState = newState;
            UpdateWebView();
        }

        private void UpdateWebView() {
            if (!_webViewObject) {
                return;
            }

            switch (_currentState) {
                case WebViewState.Hidden:
                    _webViewObject.SetMargins(0, 0, 0, _defaultScreenHeight);
                    break;
                case WebViewState.TopBar:
                    _webViewObject.SetMargins(0, 0, 0, _defaultScreenHeight - _webViewHeight);
                    break;
                case WebViewState.FullScreen:
                case WebViewState.SafeAreaBased:
                    _webViewObject.SetMargins(0, 0, 0, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _webViewObject.SetVisibility(_currentState != WebViewState.Hidden && !Application.isEditor);
        }
    }
}

#endif