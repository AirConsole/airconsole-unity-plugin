/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 *
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

using Callback = System.Action<string>;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
/// <summary>
/// Provides a Unity-compatible dispatcher that mimics <c>UnitySendMessage</c> behaviour in edit-time player builds.
/// </summary>
public class UnitySendMessageDispatcher
{
    /// <summary>
    /// Invokes <paramref name="method"/> on the scene object named <paramref name="name"/> and forwards <paramref name="message"/>.
    /// </summary>
    /// <param name="name">Target GameObject name.</param>
    /// <param name="method">Method to call on the GameObject.</param>
    /// <param name="message">Message payload forwarded to the receiver.</param>
    public static void Dispatch(string name, string method, string message) {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
            obj.SendMessage(method, message);
    }
}
#endif

/// <summary>
/// High-level wrapper around the native unity-webview plugin, exposing platform-specific WebView features to Unity.
/// </summary>
public class WebViewObject : MonoBehaviour
{
#if UNITY_ANDROID
    WebViewCallback callback;
#endif
    Callback onJS;
    Callback onError;
    Callback onHttpError;
    Callback onStarted;
    Callback onLoaded;
    Callback onHooked;
    Callback onCookies;
    Callback onAudioFocusChanged;
    bool paused;
    bool visibility;

    // Thread-safe event queue infrastructure
    private static int _mainThreadId = -1;
    private readonly ConcurrentQueue<WebViewEvent> _eventQueue = new ConcurrentQueue<WebViewEvent>();

    bool alertDialogEnabled;
    bool scrollBounceEnabled;
    int mMarginLeft;
    int mMarginTop;
    int mMarginRight;
    int mMarginBottom;
    bool mMarginRelative;
    float mMarginLeftComputed;
    float mMarginTopComputed;
    float mMarginRightComputed;
    float mMarginBottomComputed;
    bool mMarginRelativeComputed;
    /// <summary>
    /// Optional canvas used by the macOS editor/player implementation to host background visuals behind the WebView.
    /// </summary>
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    public GameObject canvas;
    Image bg;
    IntPtr webView;
    Rect rect;
    Texture2D texture;
    byte[] textureDataBuffer;
    string inputString = "";
    bool hasFocus;
#elif UNITY_ANDROID
    AndroidJavaObject webView;

    bool mAndroidInitialized;
    bool mAndroidTransparent;
    bool mAndroidZoom = true;
    string mAndroidUserAgent = "";
    int mAndroidRadius;
    int mAndroidForceDarkMode;
    bool mDestroying;
    bool mSuppressRenderProcessRecovery;
    bool mRecoveringFromRenderProcessGone;
    // Non-null while a renderer death is queued (suppressed during OnDisable/pause);
    // doubles as the "pending" flag since didCrash is never null when queued.
    string mPendingRenderProcessGoneDidCrash;
    const int mAndroidRenderProcessGoneMaxReloadAttempts = 2;
    string mLatestUrl;
    int mAndroidRenderProcessGoneReloadAttempts;
    bool mAndroidHasMargins;
    bool mAndroidHasVisibility;
    bool mAndroidDebuggingEnabled;
    bool mAndroidHasScrollbarsVisibility;
    bool mAndroidScrollbarsVisibility;
    bool mAndroidInteractionEnabled = true;
    bool mAndroidHasCameraAccess;
    bool mAndroidCameraAccess;
    bool mAndroidHasMicrophoneAccess;
    bool mAndroidMicrophoneAccess;
    bool mAndroidHasUrlPattern;
    string mAndroidAllowPattern;
    string mAndroidDenyPattern;
    string mAndroidHookPattern;
    readonly Dictionary<string, string> mAndroidCustomHeaders = new Dictionary<string, string>();
    bool mAndroidHasBasicAuthInfo;
    string mAndroidBasicAuthUserName;
    string mAndroidBasicAuthPassword;
    bool mAndroidHasTextZoom;
    int mAndroidTextZoom = 100;
    bool mVisibility;
    int mKeyboardVisibleHeight;
    float mResumedTimestamp;
    int mLastScreenHeight;
#if UNITYWEBVIEW_ANDROID_ENABLE_NAVIGATOR_ONLINE
    float androidNetworkReachabilityCheckT0 = -1.0f;
    NetworkReachability? androidNetworkReachability0 = null;
#endif

    private void CreateAndroidWebView() {
        webView = new AndroidJavaObject("net.gree.unitywebview.CWebViewPlugin");
#if UNITY_2021_1_OR_NEWER
        webView.SetStatic<bool>("forceBringToFront", true);
#endif
        webView.Call("Init", name, mAndroidTransparent, mAndroidZoom, mAndroidForceDarkMode, mAndroidUserAgent, mAndroidRadius);
        // Reassigning drops any previous proxy reference so its JNI global ref is
        // released by the finalizer on GC. AndroidJavaProxy exposes no Dispose()
        // on this Unity version, so dropping the managed reference is the only release.
        callback = new (this);
        webView.Call("SetCallback", callback);
#if !UNITY_EDITOR
        // Flush anything the native side queued before the callback was attached
        // (the only window where Java falls back to its message queue). Once the
        // callback is set it delivers every event directly, so there is no
        // per-frame polling: draining here is a one-time catch-up.
        DrainAndroidMessageQueue();
#endif
    }

    private void RecoverFromRenderProcessGone(string didCrash) {
        if (mDestroying || !mAndroidInitialized || mRecoveringFromRenderProcessGone) {
            return;
        }
        if (mSuppressRenderProcessRecovery) {
            // Coalesce to non-null: the field's non-null state is the "pending" flag.
            mPendingRenderProcessGoneDidCrash = didCrash ?? "false";
            return;
        }

        mRecoveringFromRenderProcessGone = true;
        try {
            if (webView != null) {
                webView.Call("Destroy");
                webView.Dispose();
                webView = null;
            }
            CreateAndroidWebView();
            ReplayAndroidStateAfterRecovery();
            if (ShouldReloadAfterRenderProcessGone()) {
                webView.Call("LoadURL", mLatestUrl);
            } else {
                Debug.LogWarning($"WebView render process gone (didCrash={didCrash}); automatic reload skipped.");
            }
        } catch (Exception ex) {
            Debug.LogError($"WebView render process recovery failed: {ex}");
            // Recreation failed, so webView is null and no further onRenderProcessGone
            // can retrigger recovery. Mark the instance uninitialized so a later Init()
            // rebuilds cleanly instead of every method silently no-opping forever.
            mAndroidInitialized = false;
        } finally {
            mRecoveringFromRenderProcessGone = false;
        }
    }

    private void ReplayAndroidStateAfterRecovery() {
        if (webView == null) {
            return;
        }

        // The recreated native WebView has no state. Re-apply everything the
        // caller configured by re-invoking the PUBLIC setters with the values we
        // already store, so the native call for each setting lives in exactly one
        // place (the setter) and cannot drift from a duplicate replay copy. Only
        // settings that were actually applied are replayed (the mAndroidHas*
        // gates), matching the pre-recovery behaviour.

        // SetMargins dedupes against the computed cache; clear it so the setter
        // re-applies to the new native view instead of early-returning.
        mMarginLeftComputed = mMarginTopComputed = mMarginRightComputed = mMarginBottomComputed = -9999;

        if (mAndroidHasMargins) {
            ReplayAndroidState("SetMargins", () => SetMargins(mMarginLeft, mMarginTop, mMarginRight, mMarginBottom, mMarginRelative));
        }
        if (mAndroidHasVisibility) {
            ReplayAndroidState("SetVisibility", () => SetVisibility(mVisibility));
        }
        if (mAndroidDebuggingEnabled) {
            ReplayAndroidState("EnableWebviewDebugging", () => EnableWebviewDebugging(true));
        }
        if (mAndroidHasScrollbarsVisibility) {
            ReplayAndroidState("SetScrollbarsVisibility", () => SetScrollbarsVisibility(mAndroidScrollbarsVisibility));
        }
        ReplayAndroidState("SetInteractionEnabled", () => SetInteractionEnabled(mAndroidInteractionEnabled));
        ReplayAndroidState("SetAlertDialogEnabled", () => SetAlertDialogEnabled(alertDialogEnabled));
        if (mAndroidHasCameraAccess) {
            ReplayAndroidState("SetCameraAccess", () => SetCameraAccess(mAndroidCameraAccess));
        }
        if (mAndroidHasMicrophoneAccess) {
            ReplayAndroidState("SetMicrophoneAccess", () => SetMicrophoneAccess(mAndroidMicrophoneAccess));
        }
        if (mAndroidHasUrlPattern) {
            ReplayAndroidState("SetURLPattern", () => SetURLPattern(mAndroidAllowPattern, mAndroidDenyPattern, mAndroidHookPattern));
        }
        // Snapshot the header map: AddCustomHeader writes back into it, so iterating
        // the live dictionary would throw.
        foreach (var header in new List<KeyValuePair<string, string>>(mAndroidCustomHeaders)) {
            var entry = header;
            ReplayAndroidState("AddCustomHeader", () => AddCustomHeader(entry.Key, entry.Value));
        }
        if (mAndroidHasBasicAuthInfo) {
            ReplayAndroidState("SetBasicAuthInfo", () => SetBasicAuthInfo(mAndroidBasicAuthUserName, mAndroidBasicAuthPassword));
        }
        if (mAndroidHasTextZoom) {
            ReplayAndroidState("SetTextZoom", () => SetTextZoom(mAndroidTextZoom));
        }
    }

    private void ReplayAndroidState(string name, Action replay) {
        try {
            replay();
        } catch (Exception ex) {
            Debug.LogWarning($"WebView render process recovery could not replay {name}: {ex}");
        }
    }

    private bool ShouldReloadAfterRenderProcessGone() {
        if (string.IsNullOrEmpty(mLatestUrl)) {
            return false;
        }
        // The attempt counter is reset on URL change (TrackLatestUrl) and on a
        // successful load (CallOnLoaded), so it is always scoped to mLatestUrl here.
        if (mAndroidRenderProcessGoneReloadAttempts >= mAndroidRenderProcessGoneMaxReloadAttempts) {
            Debug.LogError($"WebView render process recovery stopped after {mAndroidRenderProcessGoneReloadAttempts} reload attempts for {mLatestUrl}.");
            return false;
        }
        mAndroidRenderProcessGoneReloadAttempts++;
        return true;
    }

    private void TrackLatestUrl(string url) {
        if (!string.IsNullOrEmpty(url)) {
            if (mLatestUrl != url) {
                // NOTE: this makes the reload cap a per-*stable*-URL cap. A renderer
                // that crashes right after an alternating redirect (A->B->A->...)
                // changes the URL every cycle and so resets the budget each time,
                // recovering indefinitely. Single-URL crash loops are still capped.
                mAndroidRenderProcessGoneReloadAttempts = 0;
            }
            mLatestUrl = url;
        }
    }

    private void OnApplicationPause(bool paused) {
        // Temporarily disable pausing to ensure the event queue is processed
        this.paused = false;
        ProcessEventQueue();

        this.paused = paused;

        if (webView == null)
            return;
        webView.Call("OnApplicationPause", paused);
    }

    /// <summary>
    /// Called when the component is disabled. Flushes any remaining events in the queue.
    /// </summary>
    private void OnDisable() {
        mSuppressRenderProcessRecovery = true;
        // Flush remaining events before component destruction
        // Temporarily unpause to allow processing
        var wasPaused = paused;
        paused = false;
        ProcessEventQueue();
        paused = wasPaused;
    }

    private void OnEnable() {
        if (!mDestroying) {
            mSuppressRenderProcessRecovery = false;
            if (mPendingRenderProcessGoneDidCrash != null) {
                var didCrash = mPendingRenderProcessGoneDidCrash;
                mPendingRenderProcessGoneDidCrash = null;
                RecoverFromRenderProcessGone(didCrash);
            }
        }
    }

    /// <summary>
    /// Called when the application is about to quit. Flushes any remaining events.
    /// </summary>
    void OnApplicationQuit() {
        mSuppressRenderProcessRecovery = true;
        // Final flush before app closes
        var wasPaused = paused;
        paused = false;
        ProcessEventQueue();
        paused = wasPaused;
    }

    private void Update() {
        // NOTE:
        //
        // When OnApplicationPause(true) is called and the app is in closing, webView.Call(...)
        // after that could cause crashes because underlying java instances were closed.
        //
        // This has not been cleary confirmed yet. However, as Update() is called once after
        // OnApplicationPause(true), it is likely correct.
        //
        // Base on this assumption, we do nothing here if the app is paused.
        //
        // cf. https://github.com/gree/unity-webview/issues/991#issuecomment-1776628648
        // cf. https://docs.unity3d.com/2020.3/Documentation/Manual/ExecutionOrder.html
        //
        // In between frames
        //
        // * OnApplicationPause: This is called at the end of the frame where the pause is detected,
        //   effectively between the normal frame updates. One extra frame will be issued after
        //   OnApplicationPause is called to allow the game to show graphics that indicate the
        //   paused state.
        //
        if (paused)
            return;
        ProcessEventQueue();
        if (webView == null)
            return;
#if UNITYWEBVIEW_ANDROID_ENABLE_NAVIGATOR_ONLINE
        var t = Time.time;
        if (t - 1.0f >= androidNetworkReachabilityCheckT0) {
            androidNetworkReachabilityCheckT0 = t;
            var androidNetworkReachability = Application.internetReachability;
            if (androidNetworkReachability0 != androidNetworkReachability) {
                androidNetworkReachability0 = androidNetworkReachability;
                webView.Call("SetNetworkAvailable", androidNetworkReachability != NetworkReachability.NotReachable);
            }
        }
#endif
        if (mResumedTimestamp != 0.0f && Time.realtimeSinceStartup - mResumedTimestamp > 0.5f) {
            mResumedTimestamp = 0.0f;
            webView.Call("SetVisibility", mVisibility);
        }
        if (Screen.height != mLastScreenHeight) {
            mLastScreenHeight = Screen.height;
            webView.Call("EvaluateJS", "(function() {var e = document.activeElement; if (e != null && e.tagName.toLowerCase() != 'body') {e.blur(); e.focus();}})()");
        }

        // Process any events queued while native state was updated this frame.
        ProcessEventQueue();
    }

#if UNITY_2020_2_OR_NEWER
#else
    int mRequestPermissionPhase;

    IEnumerator RequestFileChooserPermissionsCoroutine(string[] permissions) {
        foreach (var permission in permissions) {
            mRequestPermissionPhase = 0;
            Permission.RequestUserPermission(permission);
            // waiting permission dialog that may not be opened.
            for (var i = 0; i < 8 && mRequestPermissionPhase == 0; i++) {
                yield return new WaitForSeconds(0.25f);
            }
            if (mRequestPermissionPhase == 0) {
                // permission dialog was not opened.
                continue;
            }
            while (mRequestPermissionPhase == 1) {
                yield return new WaitForSeconds(0.3f);
            }
        }
        yield return new WaitForSeconds(0.3f);
        var granted = 0;
        foreach (var permission in permissions) {
            if (Permission.HasUserAuthorizedPermission(permission)) {
                granted++;
            }
        }
        StartCoroutine(CallOnRequestFileChooserPermissionsResult(granted == permissions.Length));
    }

    void OnApplicationFocus(bool hasFocus) {
        if (hasFocus) {
            if (mRequestPermissionPhase == 1) {
                mRequestPermissionPhase = 2;
            }
        }
        else
        {
            if (mRequestPermissionPhase == 0) {
                mRequestPermissionPhase = 1;
            }
        }
    }
#endif

    private IEnumerator CallOnRequestFileChooserPermissionsResult(bool granted) {
        for (var i = 0; i < 3; i++) {
            yield return null;
        }
        webView.Call("OnRequestFileChooserPermissionsResult", granted);
    }

    /// <summary>
    /// Computes the bottom margin that keeps the WebView visible above the soft keyboard.
    /// </summary>
    /// <param name="bottom">Original bottom margin in pixels.</param>
    /// <returns>The adjusted bottom margin accounting for keyboard height.</returns>
    public int AdjustBottomMargin(int bottom) {
        if (BottomAdjustmentDisabled()) {
            return bottom;
        }
        else if (mKeyboardVisibleHeight <= 0) {
            return bottom;
        }
        else
        {
            int keyboardHeight = 0;
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var player = activity.Get<AndroidJavaObject>("mUnityPlayer"))
            using (var view = player.Call<AndroidJavaObject>("getView"))
            using (var rect = new AndroidJavaObject("android.graphics.Rect")) {
                if (view.Call<bool>("getGlobalVisibleRect", rect)) {
                    int h0 = rect.Get<int>("bottom");
                    view.Call("getWindowVisibleDisplayFrame", rect);
                    int h1 = rect.Get<int>("bottom");
                    keyboardHeight = h0 - h1;
                }
            }
            return (bottom > keyboardHeight) ? bottom : keyboardHeight;
        }
    }

    private bool BottomAdjustmentDisabled() {
#if UNITYWEBVIEW_ANDROID_FORCE_MARGIN_ADJUSTMENT_FOR_KEYBOARD
        return false;
#else
        return
            !Screen.fullScreen
            || ((Screen.autorotateToLandscapeLeft || Screen.autorotateToLandscapeRight)
                && (Screen.autorotateToPortrait || Screen.autorotateToPortraitUpsideDown));
#endif
    }
#else
    IntPtr webView;
#endif

    private void Awake() {
        // Capture the main thread ID for thread-safe event queueing
        if (_mainThreadId == -1) {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        Debug.Log($"Initializing WebViewObject v{VersionInfo.VERSION}");
        alertDialogEnabled = true;
        scrollBounceEnabled = true;
        mMarginLeftComputed = -9999;
        mMarginTopComputed = -9999;
        mMarginRightComputed = -9999;
        mMarginBottomComputed = -9999;
    }

    /// <summary>
    /// Secondary event drain called after all Update() methods have run.
    /// Catches any events queued during the current frame's Update cycle.
    /// </summary>
    private void LateUpdate() {
        ProcessEventQueue();
    }

    /// <summary>
    /// Catches any events queued during the current frame's FixedUpdate cycle.
    /// </summary>
    private void FixedUpdate() {
        ProcessEventQueue();
    }

    /// <summary>
    /// Gets a value indicating whether the soft keyboard is currently visible.
    /// </summary>
    public bool IsKeyboardVisible
    {
        get
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return mKeyboardVisibleHeight > 0;
#else
            return false;
#endif
        }
    }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport("WebView")]
    private static extern string _CWebViewPlugin_GetAppPath();
    [DllImport("WebView")]
    private static extern IntPtr _CWebViewPlugin_InitStatic(
        bool inEditor, bool useMetal);
    [DllImport("WebView")]
    private static extern IntPtr _CWebViewPlugin_Init(
        string gameObject, bool transparent, bool zoom, int width, int height, string ua, bool separated);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetRect(
        IntPtr instance, int width, int height);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetVisibility(
        IntPtr instance, bool visibility);
    [DllImport("WebView")]
    private static extern bool _CWebViewPlugin_SetURLPattern(
        IntPtr instance, string allowPattern, string denyPattern, string hookPattern);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_LoadURL(
        IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_EvaluateJS(
        IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_Progress(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern bool _CWebViewPlugin_CanGoBack(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern bool _CWebViewPlugin_CanGoForward(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_GoBack(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_GoForward(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_Reload(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SendMouseEvent(IntPtr instance, int x, int y, float deltaY, int mouseState);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SendKeyEvent(IntPtr instance, int x, int y, string keyChars, ushort keyCode, int keyState);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_Update(IntPtr instance, bool refreshBitmap, int devicePixelRatio);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_BitmapWidth(IntPtr instance);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_BitmapHeight(IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_Render(IntPtr instance, IntPtr textureBuffer);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_AddCustomHeader(IntPtr instance, string headerKey, string headerValue);
    [DllImport("WebView")]
    private static extern string _CWebViewPlugin_GetCustomHeaderValue(IntPtr instance, string headerKey);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_RemoveCustomHeader(IntPtr instance, string headerKey);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_ClearCustomHeader(IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_ClearCookies();
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SaveCookies();
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_GetCookies(IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern string _CWebViewPlugin_GetMessage(IntPtr instance);
#elif UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_init(string name);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_setMargins(string name, int left, int top, int right, int bottom);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_setVisibility(string name, bool visible);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_loadURL(string name, string url);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_evaluateJS(string name, string js);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_destroy(string name);
#endif

    /// <summary>
    /// Determines whether the current platform exposes a compatible WebView implementation.
    /// </summary>
    /// <returns><c>true</c> when the underlying native plugin can be instantiated; otherwise <c>false</c>.</returns>
    public static bool IsWebViewAvailable() {
#if !UNITY_EDITOR && UNITY_ANDROID
        using (var plugin = new AndroidJavaObject("net.gree.unitywebview.CWebViewPlugin")) {
            return plugin.CallStatic<bool>("IsWebViewAvailable");
        }
#else
        return true;
#endif
    }

    /// <summary>
    /// Initialises the platform WebView instance and binds callbacks for native-to-Unity messaging.
    /// </summary>
    /// <param name="cb">Invoked when JavaScript calls <c>Unity.call</c>.</param>
    /// <param name="err">Invoked when the WebView reports a navigation error.</param>
    /// <param name="httpErr">Invoked when the WebView receives an HTTP error status.</param>
    /// <param name="ld">Invoked after the WebView finishes loading a page.</param>
    /// <param name="started">Invoked when the WebView starts navigating to a page.</param>
    /// <param name="hooked">Invoked when a hooked URL pattern is hit.</param>
    /// <param name="cookies">Invoked when cookies are requested from the WebView.</param>
    /// <param name="transparent">Whether the WebView background should be transparent.</param>
    /// <param name="zoom">Whether native zoom controls are enabled.</param>
    /// <param name="ua">Optional custom user agent string.</param>
    /// <param name="radius">Rounded corner radius (Android only).</param>
    /// <param name="androidForceDarkMode">Android dark-mode override (0 = system, 1 = off, 2 = on).</param>
    /// <param name="separated">Creates a separate native window in the Unity editor.</param>
    /// <param name="audioFocusChanged">Receives Android audio focus transition events.</param>
    public void Init(
        Callback cb = null,
        Callback err = null,
        Callback httpErr = null,
        Callback ld = null,
        Callback started = null,
        Callback hooked = null,
        Callback cookies = null,
        bool transparent = false,
        bool zoom = true,
        string ua = "",
        int radius = 0,
        // android
        int androidForceDarkMode = 0,  // 0: follow system setting, 1: force dark off, 2: force dark on
        // editor
        bool separated = false) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        _CWebViewPlugin_InitStatic(
            Application.platform == RuntimePlatform.OSXEditor,
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal);
#endif
        onJS = cb;
        onError = err;
        onHttpError = httpErr;
        onStarted = started;
        onLoaded = ld;
        onHooked = hooked;
        onCookies = cookies;
#if UNITY_WEBGL
#if !UNITY_EDITOR
        _gree_unity_webview_init(name);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.init", name);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        Debug.LogError("Webview is not supported on this platform.");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        {
            var uri = new Uri(_CWebViewPlugin_GetAppPath());
            var info = File.ReadAllText(uri.LocalPath + "Contents/Info.plist");
            if (Regex.IsMatch(info, @"<key>CFBundleGetInfoString</key>\s*<string>Unity version [5-9]\.[3-9]")
                && !Regex.IsMatch(info, @"<key>NSAppTransportSecurity</key>\s*<dict>\s*<key>NSAllowsArbitraryLoads</key>\s*<true/>\s*</dict>")) {
                Debug.LogWarning("<color=yellow>WebViewObject: NSAppTransportSecurity isn't configured to allow HTTP. If you need to allow any HTTP access, please shutdown Unity and invoke:</color>\n/usr/libexec/PlistBuddy -c \"Add NSAppTransportSecurity:NSAllowsArbitraryLoads bool true\" /Applications/Unity/Unity.app/Contents/Info.plist");
            }
        }
#if UNITY_EDITOR_OSX
        // if (string.IsNullOrEmpty(ua)) {
        //     ua = @"Mozilla/5.0 (iPhone; CPU iPhone OS 7_1_2 like Mac OS X) AppleWebKit/537.51.2 (KHTML, like Gecko) Version/7.0 Mobile/11D257 Safari/9537.53";
        // }
#endif
        webView = _CWebViewPlugin_Init(
            name,
            transparent,
            zoom,
            Screen.width,
            Screen.height,
            ua
#if UNITY_EDITOR
            , separated
#else
            , false
#endif
            );
        rect = new Rect(0, 0, Screen.width, Screen.height);
#elif UNITY_ANDROID
        mAndroidInitialized = true;
        mDestroying = false;
        mSuppressRenderProcessRecovery = false;
        // Clear any recovery state carried over from a prior Init on a reused
        // instance, so a queued (suppressed) renderer death can't later replay
        // against the freshly created view and reload a stale URL.
        mPendingRenderProcessGoneDidCrash = null;
        mLatestUrl = null;
        mAndroidRenderProcessGoneReloadAttempts = 0;
        mAndroidTransparent = transparent;
        mAndroidZoom = zoom;
        mAndroidForceDarkMode = androidForceDarkMode;
        mAndroidUserAgent = ua;
        mAndroidRadius = radius;
        CreateAndroidWebView();
#else
        Debug.LogError("Webview is not supported on this platform.");
#endif
    }

    private void OnDestroy() {
#if UNITY_WEBGL
#if !UNITY_EDITOR
        _gree_unity_webview_destroy(name);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.destroy", name);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (bg != null) {
            Destroy(bg.gameObject);
        }
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_Destroy(webView);
        webView = IntPtr.Zero;
        Destroy(texture);
#elif UNITY_ANDROID
        mDestroying = true;
        mSuppressRenderProcessRecovery = true;
        if (webView == null)
            return;
        webView.Call("Destroy");
        webView.Dispose();
        webView = null;
        // AndroidJavaProxy has no Dispose() here; drop the reference so GC can
        // finalize it and release its JNI global ref.
        callback = null;
#endif
    }

    /// <summary>
    /// Pauses WebView timers and rendering to match Unity's lifecycle.
    /// </summary>
    public void Pause() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        //TODO: UNSUPPORTED
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("Pause");
#endif
    }

    /// <summary>
    /// Resumes WebView timers previously paused via <see cref="Pause"/>.
    /// </summary>
    public void Resume() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        //TODO: UNSUPPORTED
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("Resume");
#endif
    }

    /// <summary>
    /// Convenience helper that positions the WebView using a center point and size instead of raw margins.
    /// </summary>
    /// <param name="center">Desired centre position in screen pixels (historically anchored to lower-left).</param>
    /// <param name="scale">Desired width and height of the WebView in pixels.</param>
    public void SetCenterPositionWithScale(Vector2 center, Vector2 scale) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#else
        float left = (Screen.width - scale.x) / 2.0f + center.x;
        float right = Screen.width - (left + scale.x);
        float bottom = (Screen.height - scale.y) / 2.0f + center.y;
        float top = Screen.height - (bottom + scale.y);
        SetMargins((int)left, (int)top, (int)right, (int)bottom);
#endif
    }

    /// <summary>
    /// Applies absolute or relative margins to the WebView rectangle.
    /// </summary>
    /// <param name="left">Left margin in pixels or percentage.</param>
    /// <param name="top">Top margin in pixels or percentage.</param>
    /// <param name="right">Right margin in pixels or percentage.</param>
    /// <param name="bottom">Bottom margin in pixels or percentage.</param>
    /// <param name="relative">When <c>true</c>, margins are interpreted as percentages of the screen size.</param>
    public void SetMargins(int left, int top, int right, int bottom, bool relative = false) {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        return;
#elif UNITY_WEBPLAYER || UNITY_WEBGL
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
#elif UNITY_ANDROID
        if (webView == null)
            return;
#endif

        mMarginLeft = left;
        mMarginTop = top;
        mMarginRight = right;
        mMarginBottom = bottom;
        mMarginRelative = relative;
#if UNITY_ANDROID && !UNITY_EDITOR
        mAndroidHasMargins = true;
#endif
        float ml, mt, mr, mb;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_WEBPLAYER || UNITY_WEBGL
        ml = left;
        mt = top;
        mr = right;
        mb = bottom;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        ml = left;
        mt = top;
        mr = right;
        mb = bottom;
#elif UNITY_ANDROID
        if (relative) {
            float w = (float)Screen.width;
            float h = (float)Screen.height;
            int iw = Display.main.systemWidth;
            int ih = Display.main.systemHeight;
            if (!Screen.fullScreen) {
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var player = activity.Get<AndroidJavaObject>("mUnityPlayer"))
                using (var view = player.Call<AndroidJavaObject>("getView"))
                using (var rect = new AndroidJavaObject("android.graphics.Rect")) {
                    view.Call("getDrawingRect", rect);
                    iw = rect.Call<int>("width");
                    ih = rect.Call<int>("height");
                }
            }
            ml = left / w * iw;
            mt = top / h * ih;
            mr = right / w * iw;
            mb = AdjustBottomMargin((int)(bottom / h * ih));
        }
        else
        {
            ml = left;
            mt = top;
            mr = right;
            mb = AdjustBottomMargin(bottom);
        }
#endif
        bool r = relative;

        if (ml == mMarginLeftComputed
            && mt == mMarginTopComputed
            && mr == mMarginRightComputed
            && mb == mMarginBottomComputed
            && r == mMarginRelativeComputed) {
            return;
        }
        mMarginLeftComputed = ml;
        mMarginTopComputed = mt;
        mMarginRightComputed = mr;
        mMarginBottomComputed = mb;
        mMarginRelativeComputed = r;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.setMargins", name, (int)ml, (int)mt, (int)mr, (int)mb);
#elif UNITY_WEBGL && !UNITY_EDITOR
        _gree_unity_webview_setMargins(name, (int)ml, (int)mt, (int)mr, (int)mb);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        int width = (int)(Screen.width - (ml + mr));
        int height = (int)(Screen.height - (mb + mt));
        _CWebViewPlugin_SetRect(webView, width, height);
        rect = new Rect(left, bottom, width, height);
        UpdateBGTransform();
#elif UNITY_ANDROID
        webView.Call("SetMargins", (int)ml, (int)mt, (int)mr, (int)mb);
#endif
    }

    /// <summary>
    /// Shows or hides the WebView while keeping its state intact.
    /// </summary>
    /// <param name="v"><c>true</c> to make the WebView visible; otherwise <c>false</c>.</param>
    public void SetVisibility(bool v) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (bg != null) {
            bg.gameObject.SetActive(v);
        }
#endif
        if (GetVisibility() && !v) {
            EvaluateJS("if (document && document.activeElement) document.activeElement.blur();");
        }
#if UNITY_WEBGL
#if !UNITY_EDITOR
        _gree_unity_webview_setVisibility(name, v);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.setVisibility", name, v);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_SetVisibility(webView, v);
#elif UNITY_ANDROID
        mVisibility = v;
        mAndroidHasVisibility = true;
        if (webView == null)
            return;
        webView.Call("SetVisibility", v);
#endif
        visibility = v;
    }

    /// <summary>
    /// Gets the last visibility flag applied to the WebView.
    /// </summary>
    public bool GetVisibility() {
        return visibility;
    }

    /// <summary>
    /// Toggles native scroll bar rendering where supported.
    /// </summary>
    /// <param name="v"><c>true</c> to show scroll bars; otherwise <c>false</c>.</param>
    public void SetScrollbarsVisibility(bool v) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // TODO: UNSUPPORTED
#elif UNITY_ANDROID
        mAndroidHasScrollbarsVisibility = true;
        mAndroidScrollbarsVisibility = v;
        if (webView == null)
            return;
        webView.Call("SetScrollbarsVisibility", v);
#else
        // TODO: UNSUPPORTED
#endif
    }

    /// <summary>
    /// Enables the platform's WebView remote debugging facilities when available (Android only).
    /// </summary>
    /// <param name="enabled">Whether debugging should be enabled.</param>
    public void EnableWebviewDebugging(bool enabled) {
#if UNITY_ANDROID && !UNITY_EDITOR
        mAndroidDebuggingEnabled = enabled;
        if (webView == null) {
            return;
        }

        webView.Call("enableWebViewDebugging", enabled);
#else
        Debug.Log($"EnableWebviewDebugging({enabled}) not implemented on {Application.platform}");
#endif
    }

    /// <summary>
    /// Enables or disables user interaction with the WebView surface.
    /// </summary>
    /// <param name="enabled">Whether touch input is forwarded to the WebView.</param>
    public void SetInteractionEnabled(bool enabled) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // TODO: UNSUPPORTED
#elif UNITY_ANDROID
        mAndroidInteractionEnabled = enabled;
        if (webView == null)
            return;
        webView.Call("SetInteractionEnabled", enabled);
#else
        // TODO: UNSUPPORTED
#endif
    }

    /// <summary>
    /// Controls whether JavaScript alert/confirm/prompt dialogs are permitted.
    /// </summary>
    /// <param name="e"><c>true</c> to allow dialogs; otherwise <c>false</c>.</param>
    public void SetAlertDialogEnabled(bool e) {
        alertDialogEnabled = e;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // TODO: UNSUPPORTED
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("SetAlertDialogEnabled", e);
#else
        // TODO: UNSUPPORTED
#endif
    }

    /// <summary>
    /// Gets the cached alert dialog enable flag.
    /// </summary>
    public bool GetAlertDialogEnabled() {
        return alertDialogEnabled;
    }

    /// <summary>
    /// Toggles bouncing/elastic scrolling on supported platforms.
    /// </summary>
    /// <param name="e"><c>true</c> to enable bouncing, otherwise <c>false</c>.</param>
    public void SetScrollBounceEnabled(bool e) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // TODO: UNSUPPORTED
#elif UNITY_ANDROID
        // TODO: UNSUPPORTED
#else
        // TODO: UNSUPPORTED
#endif
        scrollBounceEnabled = e;
    }

    /// <summary>
    /// Gets the cached bounce/elastic scrolling flag.
    /// </summary>
    public bool GetScrollBounceEnabled() {
        return scrollBounceEnabled;
    }

    /// <summary>
    /// Grants or revokes camera access for WebRTC and file input elements.
    /// </summary>
    /// <param name="allowed">Whether the WebView should expose camera capture to web content.</param>
    public void SetCameraAccess(bool allowed) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // TODO: UNSUPPORTED
#elif UNITY_ANDROID
        mAndroidHasCameraAccess = true;
        mAndroidCameraAccess = allowed;
        if (webView == null)
            return;
        webView.Call("SetCameraAccess", allowed);
#else
        // TODO: UNSUPPORTED
#endif
    }

    /// <summary>
    /// Grants or revokes microphone access for WebRTC and audio capture flows.
    /// </summary>
    /// <param name="allowed">Whether the WebView should expose microphone capture to web content.</param>
    public void SetMicrophoneAccess(bool allowed) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // TODO: UNSUPPORTED
#elif UNITY_ANDROID
        mAndroidHasMicrophoneAccess = true;
        mAndroidMicrophoneAccess = allowed;
        if (webView == null)
            return;
        webView.Call("SetMicrophoneAccess", allowed);
#else
        // TODO: UNSUPPORTED
#endif
    }

    /// <summary>
    /// Forces the Android plugin to request audio focus back for Unity's audio subsystem.
    /// </summary>
    public void RequestUnityAudioFocus() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (webView == null)
            return;
        webView.Call("requestUnityAudioFocus");
#endif
    }

    /// <summary>
    /// Relinquishes Unity's audio focus so WebView media can take control.
    /// </summary>
    public void AbandonUnityAudioFocus() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (webView == null)
            return;
        webView.Call("abandonUnityAudioFocus");
#endif
    }

    /// <summary>
    /// Sets allow/deny/hook regular expressions to control navigation handling.
    /// </summary>
    /// <param name="allowPattern">Regex pattern for URLs that are allowed.</param>
    /// <param name="denyPattern">Regex pattern for URLs that should be blocked.</param>
    /// <param name="hookPattern">Regex pattern that triggers hook callbacks.</param>
    /// <returns><c>true</c> if the operation is supported on the current platform.</returns>
    public bool SetURLPattern(string allowPattern, string denyPattern, string hookPattern) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
        return false;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        return false;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return false;
        return _CWebViewPlugin_SetURLPattern(webView, allowPattern, denyPattern, hookPattern);
#elif UNITY_ANDROID
        mAndroidHasUrlPattern = true;
        mAndroidAllowPattern = allowPattern;
        mAndroidDenyPattern = denyPattern;
        mAndroidHookPattern = hookPattern;
        if (webView == null)
            return false;
        return webView.Call<bool>("SetURLPattern", allowPattern, denyPattern, hookPattern);
#endif
    }

    /// <summary>
    /// Navigates the WebView to the specified URL.
    /// </summary>
    /// <param name="url">Absolute or relative URL to load.</param>
    public void LoadURL(string url) {
        if (string.IsNullOrEmpty(url))
            return;
#if UNITY_ANDROID && !UNITY_EDITOR
        TrackLatestUrl(url);
#endif
#if UNITY_WEBGL
#if !UNITY_EDITOR
        _gree_unity_webview_loadURL(name, url);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.loadURL", name, url);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_LoadURL(webView, url);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("LoadURL", url);
#endif
    }

    /// <summary>
    /// Evaluates JavaScript inside the current WebView context.
    /// </summary>
    /// <param name="js">Script source to execute.</param>
    public void EvaluateJS(string js) {
#if UNITY_WEBGL
#if !UNITY_EDITOR
        _gree_unity_webview_evaluateJS(name, js);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.evaluateJS", name, js);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_EvaluateJS(webView, js);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("EvaluateJS", js);
#endif
    }

    /// <summary>
    /// Returns the current navigation progress percentage where supported.
    /// </summary>
    public int Progress() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
        return 0;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        return 0;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return 0;
        return _CWebViewPlugin_Progress(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return 0;
        return webView.Get<int>("progress");
#endif
    }

    /// <summary>
    /// Returns whether the WebView has a previous page in its navigation history.
    /// </summary>
    public bool CanGoBack() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
        return false;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        return false;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return false;
        return _CWebViewPlugin_CanGoBack(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return false;
        return webView.Get<bool>("canGoBack");
#endif
    }

    /// <summary>
    /// Returns whether the WebView can navigate forward in its history stack.
    /// </summary>
    public bool CanGoForward() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
        return false;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        return false;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return false;
        return _CWebViewPlugin_CanGoForward(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return false;
        return webView.Get<bool>("canGoForward");
#endif
    }

    /// <summary>
    /// Navigates to the previous entry in the WebView history if available.
    /// </summary>
    public void GoBack() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_GoBack(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("GoBack");
#endif
    }

    /// <summary>
    /// Navigates to the next entry in the WebView history if available.
    /// </summary>
    public void GoForward() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_GoForward(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("GoForward");
#endif
    }

    /// <summary>
    /// Reloads the current WebView page.
    /// </summary>
    public void Reload() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_Reload(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("Reload");
#endif
    }

    #region Thread-Safe Event Queue

    /// <summary>
    /// Checks if the current thread is the Unity main thread.
    /// </summary>
    /// <returns>True if executing on the main thread, false otherwise.</returns>
    private bool IsMainThread() {
        if (_mainThreadId == -1)
            return false; // Defensive: queue if main thread ID not yet captured
        return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
    }

    /// <summary>
    /// Processes all queued events on the main thread.
    /// Called from Update(), LateUpdate(), and lifecycle hooks.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    private void ProcessEventQueue() {
        if (paused)
            return;

        WebViewEvent evt;
        while (_eventQueue.TryDequeue(out evt)) {
            try
            {
                DispatchEvent(evt);
            }
            catch (Exception ex) {
                Debug.LogError($"WebView event handler exception for {evt.Type}: {ex}");
                // Continue processing remaining events
            }
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // One-time catch-up drain of the native fallback queue, called right after the
    // callback is attached (see CreateAndroidWebView). Not called per frame: the
    // callback delivers events directly, so steady-state polling would just marshal
    // null every frame.
    private void DrainAndroidMessageQueue() {
        if (webView == null) {
            return;
        }

        for (;;) {
            string message;
            try {
                message = webView.Call<string>("GetMessage");
            } catch (Exception ex) {
                Debug.LogWarning($"WebView message queue drain failed: {ex}");
                break;
            }
            if (message == null) {
                break;
            }
            EnqueueEvent(message);
        }
    }
#endif

    /// <summary>
    /// Dispatches a single event to the appropriate callback.
    /// </summary>
    /// <param name="evt">The event to dispatch.</param>
    private void DispatchEvent(WebViewEvent evt) {
        switch (evt.Type) {
            case WebViewEvent.EventType.Message:
                CallFromJS(evt.Payload);
                break;
            case WebViewEvent.EventType.Error:
                CallOnError(evt.Payload);
                break;
            case WebViewEvent.EventType.HttpError:
                CallOnHttpError(evt.Payload);
                break;
            case WebViewEvent.EventType.Started:
                CallOnStarted(evt.Payload);
                break;
            case WebViewEvent.EventType.Loaded:
                CallOnLoaded(evt.Payload);
                break;
            case WebViewEvent.EventType.Hooked:
                CallOnHooked(evt.Payload);
                break;
            case WebViewEvent.EventType.Cookies:
                CallOnCookies(evt.Payload);
                break;
            case WebViewEvent.EventType.AudioFocusChanged:
                CallOnAudioFocusChanged(evt.Payload);
                break;
            case WebViewEvent.EventType.KeyboardHeightChanged:
                SetKeyboardVisible(evt.Payload);
                break;
            case WebViewEvent.EventType.FileChooserPermissions:
                RequestFileChooserPermissions();
                break;
#if UNITY_ANDROID
            case WebViewEvent.EventType.RenderProcessGone:
                CallOnRenderProcessGone(evt.Payload);
                break;
#endif
            case WebViewEvent.EventType.Unknown:
                Debug.LogWarning($"Unknown WebView event received: {evt.Payload}");
                break;
        }
    }

    /// <summary>
    /// Enqueues an event from a native bridge message string.
    /// Thread-safe - can be called from any thread.
    /// </summary>
    /// <param name="message">The raw message in "Type:Payload" format.</param>
    public void EnqueueEvent(string message) {
        var evt = WebViewEvent.FromNativeMessage(message);
        if (evt != null) {
            _eventQueue.Enqueue(evt);
        }
    }

    /// <summary>
    /// Enqueues a WebViewEvent directly.
    /// Thread-safe - can be called from any thread.
    /// </summary>
    /// <param name="evt">The event to enqueue.</param>
    public void EnqueueEvent(WebViewEvent evt) {
        if (evt != null) {
            _eventQueue.Enqueue(evt);
        }
    }

    /// <summary>
    /// Gets the current number of events in the queue.
    /// Useful for debugging and performance monitoring.
    /// </summary>
    public int EventQueueCount => _eventQueue.Count;

    #endregion

    /// <summary>
    /// Invokes the registered error callback with the supplied message.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="error">Descriptive error message.</param>
    public void CallOnError(string error) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.Error(error));
            return;
        }
        if (onError != null) {
            onError(error);
        }
    }

    /// <summary>
    /// Invokes the registered HTTP error callback for the given status information.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="error">HTTP error payload (status code or detail string).</param>
    public void CallOnHttpError(string error) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.HttpError(error));
            return;
        }
        if (onHttpError != null) {
            onHttpError(error);
        }
    }

    /// <summary>
    /// Forwards navigation-start notifications to the Unity listener.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="url">URL that began loading.</param>
    public void CallOnStarted(string url) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.Started(url));
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        TrackLatestUrl(url);
#endif
        if (onStarted != null) {
            onStarted(url);
        }
    }

    /// <summary>
    /// Forwards navigation-complete notifications to the Unity listener.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="url">URL that finished loading.</param>
    public void CallOnLoaded(string url) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.Loaded(url));
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        TrackLatestUrl(url);
        // A completed load proves the current URL is viable again, so refresh the
        // renderer-recovery reload budget. Without this the per-URL attempt cap is
        // never reset on success, and transient renderer deaths spread across a long
        // session permanently stop recovering the same URL.
        mAndroidRenderProcessGoneReloadAttempts = 0;
#endif
        if (onLoaded != null) {
            onLoaded(url);
        }
    }

    /// <summary>
    /// Handles Android WebView renderer death and recreates the native WebView on the Unity main thread.
    /// </summary>
    /// <param name="didCrash">String boolean from Android RenderProcessGoneDetail.didCrash().</param>
    public void CallOnRenderProcessGone(string didCrash) {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.RenderProcessGone(didCrash));
            return;
        }
        RecoverFromRenderProcessGone(didCrash);
#endif
    }

    /// <summary>
    /// Dispatches JavaScript messages received from the native bridge to managed listeners.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="message">Message payload supplied by the page.</param>
    public void CallFromJS(string message) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.Message(message));
            return;
        }
        if (onJS != null) {
#if !UNITY_ANDROID
#if UNITY_2018_4_OR_NEWER
            message = UnityWebRequest.UnEscapeURL(message);
#else // UNITY_2018_4_OR_NEWER
            message = WWW.UnEscapeURL(message);
#endif // UNITY_2018_4_OR_NEWER
#endif // !UNITY_ANDROID
            onJS(message);
        }
    }

    /// <summary>
    /// Dispatches URL-hook notifications to managed listeners.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="message">Hooked URL reported by the native layer.</param>
    public void CallOnHooked(string message) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.Hooked(message));
            return;
        }
        if (onHooked != null) {
#if !UNITY_ANDROID
#if UNITY_2018_4_OR_NEWER
            message = UnityWebRequest.UnEscapeURL(message);
#else // UNITY_2018_4_OR_NEWER
            message = WWW.UnEscapeURL(message);
#endif // UNITY_2018_4_OR_NEWER
#endif // !UNITY_ANDROID
            onHooked(message);
        }
    }

    /// <summary>
    /// Delivers cookie information retrieved from the WebView.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="cookies">Cookie string in standard HTTP header format.</param>
    public void CallOnCookies(string cookies) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.Cookies(cookies));
            return;
        }
        if (onCookies != null) {
            onCookies(cookies);
        }
    }

    /// <summary>
    /// Dispatches audio focus state transitions emitted by the Android plugin.
    /// Thread-safe: if called from a background thread, the event is queued for main thread processing.
    /// </summary>
    /// <param name="state">State identifier such as <c>webview-start</c> or <c>unity-gain</c>.</param>
    public void CallOnAudioFocusChanged(string state) {
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.AudioFocusChanged(state));
            return;
        }
        if (onAudioFocusChanged != null) {
            onAudioFocusChanged(state);
        }
    }

    /// <summary>
    /// Updates the tracked keyboard height when the native plugin reports visibility changes.
    /// </summary>
    /// <param name="keyboardVisibleHeight">Keyboard height, in pixels, supplied by the Android plugin.</param>
    public void SetKeyboardVisible(string keyboardVisibleHeight) {
#if !UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
        return;
#else
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.KeyboardHeightChanged(keyboardVisibleHeight));
            return;
        }
        if (BottomAdjustmentDisabled()) {
            return;
        }
        var keyboardVisibleHeight0 = mKeyboardVisibleHeight;
        var keyboardVisibleHeight1 = Int32.Parse(keyboardVisibleHeight);
        if (keyboardVisibleHeight0 != keyboardVisibleHeight1) {
            mKeyboardVisibleHeight = keyboardVisibleHeight1;
            SetMargins(mMarginLeft, mMarginTop, mMarginRight, mMarginBottom, mMarginRelative);
        }
#endif
    }

    /// <summary>
    /// Requests runtime storage permissions required by the Android file chooser implementation.
    /// </summary>
    public void RequestFileChooserPermissions() {
#if !UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
        return;
#else
        if (!IsMainThread()) {
            EnqueueEvent(WebViewEvent.FileChooserPermissions());
            return;
        }
        var permissions = new List<string>();
        using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
            if (version.GetStatic<int>("SDK_INT") >= 33) {
                if (!Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_IMAGES")) {
                    permissions.Add("android.permission.READ_MEDIA_IMAGES");
                }
                if (!Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_VIDEO")) {
                    permissions.Add("android.permission.READ_MEDIA_VIDEO");
                }
                if (!Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_AUDIO")) {
                    permissions.Add("android.permission.READ_MEDIA_AUDIO");
                }
            }
            else
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)) {
                    permissions.Add(Permission.ExternalStorageRead);
                }
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)) {
                    permissions.Add(Permission.ExternalStorageWrite);
                }
            }
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            permissions.Add(Permission.Camera);
        }
        if (permissions.Count > 0) {
#if UNITY_2020_2_OR_NEWER
            var grantedCount = 0;
            var deniedCount = 0;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (permission) =>
            {
                grantedCount++;
                if (grantedCount + deniedCount == permissions.Count) {
                    StartCoroutine(CallOnRequestFileChooserPermissionsResult(grantedCount == permissions.Count));
                }
            };
            callbacks.PermissionDenied += (permission) =>
            {
                deniedCount++;
                if (grantedCount + deniedCount == permissions.Count) {
                    StartCoroutine(CallOnRequestFileChooserPermissionsResult(grantedCount == permissions.Count));
                }
            };
            callbacks.PermissionDeniedAndDontAskAgain += (permission) =>
            {
                deniedCount++;
                if (grantedCount + deniedCount == permissions.Count) {
                    StartCoroutine(CallOnRequestFileChooserPermissionsResult(grantedCount == permissions.Count));
                }
            };
            Permission.RequestUserPermissions(permissions.ToArray(), callbacks);
#else
            StartCoroutine(RequestFileChooserPermissionsCoroutine(permissions.ToArray()));
#endif
        }
        else
        {
            StartCoroutine(CallOnRequestFileChooserPermissionsResult(true));
        }
#endif
    }

    /// <summary>
    /// Overrides the audio focus change callback at runtime.
    /// </summary>
    /// <param name="cb">Callback invoked for audio focus transitions.</param>
    public void SetOnAudioFocusChanged(Callback cb) {
        onAudioFocusChanged = cb;
    }

    /// <summary>
    /// Adds or replaces a custom HTTP request header for subsequent WebView navigations.
    /// </summary>
    /// <param name="headerKey">HTTP header key.</param>
    /// <param name="headerValue">Header value.</param>
    public void AddCustomHeader(string headerKey, string headerValue) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_AddCustomHeader(webView, headerKey, headerValue);
#elif UNITY_ANDROID
        if (!string.IsNullOrEmpty(headerKey)) {
            mAndroidCustomHeaders[headerKey] = headerValue;
        }
        if (webView == null)
            return;
        webView.Call("AddCustomHeader", headerKey, headerValue);
#endif
    }

    /// <summary>
    /// Retrieves a previously registered custom header value, if present.
    /// </summary>
    /// <param name="headerKey">HTTP header key to query.</param>
    /// <returns>The stored header value or <c>null</c> if none is found.</returns>
    public string GetCustomHeaderValue(string headerKey) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
        return null;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        return null;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return null;
        return _CWebViewPlugin_GetCustomHeaderValue(webView, headerKey);
#elif UNITY_ANDROID
        if (webView == null)
            return null;
        return webView.Call<string>("GetCustomHeaderValue", headerKey);
#endif
    }

    /// <summary>
    /// Removes a custom header so it is no longer appended to web requests.
    /// </summary>
    /// <param name="headerKey">HTTP header key to remove.</param>
    public void RemoveCustomHeader(string headerKey) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_RemoveCustomHeader(webView, headerKey);
#elif UNITY_ANDROID
        if (!string.IsNullOrEmpty(headerKey)) {
            mAndroidCustomHeaders.Remove(headerKey);
        }
        if (webView == null)
            return;
        webView.Call("RemoveCustomHeader", headerKey);
#endif
    }

    /// <summary>
    /// Clears all previously added custom headers.
    /// </summary>
    public void ClearCustomHeader() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_ClearCustomHeader(webView);
#elif UNITY_ANDROID
        mAndroidCustomHeaders.Clear();
        if (webView == null)
            return;
        webView.Call("ClearCustomHeader");
#endif
    }

    /// <summary>
    /// Deletes persistent WebView cookies where supported.
    /// </summary>
    public void ClearCookies() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_ClearCookies();
#elif UNITY_ANDROID && !UNITY_EDITOR
        if (webView == null)
            return;
        webView.Call("ClearCookies");
#endif
    }


    /// <summary>
    /// Flushes the in-memory cookie store to disk.
    /// </summary>
    public void SaveCookies() {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_SaveCookies();
#elif UNITY_ANDROID && !UNITY_EDITOR
        if (webView == null)
            return;
        webView.Call("SaveCookies");
#endif
    }


    /// <summary>
    /// Requests the cookie string for a given URL. Result is returned via <see cref="CallOnCookies"/>.
    /// </summary>
    /// <param name="url">URL whose cookies should be retrieved.</param>
    public void GetCookies(string url) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_GetCookies(webView, url);
#elif UNITY_ANDROID && !UNITY_EDITOR
        if (webView == null)
            return;
        webView.Call("GetCookies", url);
#else
        //TODO: UNSUPPORTED
#endif
    }

    /// <summary>
    /// Supplies basic authentication credentials for upcoming requests.
    /// </summary>
    /// <param name="userName">HTTP basic auth user name.</param>
    /// <param name="password">HTTP basic auth password.</param>
    public void SetBasicAuthInfo(string userName, string password) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        //TODO: UNSUPPORTED
#elif UNITY_ANDROID
        mAndroidHasBasicAuthInfo = true;
        mAndroidBasicAuthUserName = userName;
        mAndroidBasicAuthPassword = password;
        if (webView == null)
            return;
        webView.Call("SetBasicAuthInfo", userName, password);
#endif
    }

    /// <summary>
    /// Clears the WebView cache, optionally including disk-backed resources.
    /// </summary>
    /// <param name="includeDiskFiles">When <c>true</c>, disk cache entries are also removed.</param>
    public void ClearCache(bool includeDiskFiles) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_ANDROID && !UNITY_EDITOR
        if (webView == null)
            return;
        webView.Call("ClearCache", includeDiskFiles);
#endif
    }


    /// <summary>
    /// Adjusts the Android text zoom scaling factor (100 is default size).
    /// </summary>
    /// <param name="textZoom">Text zoom percentage.</param>
    public void SetTextZoom(int textZoom) {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_ANDROID && !UNITY_EDITOR
        mAndroidHasTextZoom = true;
        mAndroidTextZoom = textZoom;
        if (webView == null)
            return;
        webView.Call("SetTextZoom", textZoom);
#endif
    }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    void OnApplicationFocus(bool focus) {
        if (!focus) {
            hasFocus = false;
        }
    }

    void Start() {
        if (canvas != null) {
            var g = new GameObject(gameObject.name + "BG");
            g.transform.parent = canvas.transform;
            bg = g.AddComponent<Image>();
            UpdateBGTransform();
        }
    }

    void Update() {
        if (bg != null) {
            bg.transform.SetAsLastSibling();
        }
        if (hasFocus) {
            inputString += Input.inputString;
        }
        for (;;) {
            if (webView == IntPtr.Zero)
                break;
            string s = _CWebViewPlugin_GetMessage(webView);
            if (s == null)
                break;
            var i = s.IndexOf(':', 0);
            if (i == -1)
                continue;
            switch (s.Substring(0, i)) {
            case "CallFromJS":
                CallFromJS(s.Substring(i + 1));
                break;
            case "CallOnError":
                CallOnError(s.Substring(i + 1));
                break;
            case "CallOnHttpError":
                CallOnHttpError(s.Substring(i + 1));
                break;
            case "CallOnLoaded":
                CallOnLoaded(s.Substring(i + 1));
                break;
            case "CallOnStarted":
                CallOnStarted(s.Substring(i + 1));
                break;
            case "CallOnHooked":
                CallOnHooked(s.Substring(i + 1));
                break;
            case "CallOnCookies":
                CallOnCookies(s.Substring(i + 1));
                break;
            }
        }

        // Process any events queued from background threads
        ProcessEventQueue();

        if (webView == IntPtr.Zero || !visibility)
            return;
        bool refreshBitmap = (Time.frameCount % bitmapRefreshCycle == 0);
        _CWebViewPlugin_Update(webView, refreshBitmap, devicePixelRatio);
        if (refreshBitmap) {
            {
                var w = _CWebViewPlugin_BitmapWidth(webView);
                var h = _CWebViewPlugin_BitmapHeight(webView);
                if (texture == null || texture.width != w || texture.height != h) {
                    bool isLinearSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    texture = new Texture2D(w, h, TextureFormat.RGBA32, false, !isLinearSpace);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    textureDataBuffer = new byte[w * h * 4];
                }
            }
            if (textureDataBuffer.Length > 0) {
                var gch = GCHandle.Alloc(textureDataBuffer, GCHandleType.Pinned);
                _CWebViewPlugin_Render(webView, gch.AddrOfPinnedObject());
                gch.Free();
                texture.LoadRawTextureData(textureDataBuffer);
                texture.Apply();
            }
        }
    }

    void UpdateBGTransform() {
        if (bg != null) {
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.zero;
            bg.rectTransform.pivot = Vector2.zero;
            bg.rectTransform.position = rect.min;
            bg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.size.x);
            bg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.size.y);
        }
    }

    /// <summary>
    /// Frame interval between offscreen bitmap refreshes for the macOS implementation.
    /// </summary>
    public int bitmapRefreshCycle = 1;

    /// <summary>
    /// Device pixel ratio passed to the native renderer when updating the texture.
    /// </summary>
    public int devicePixelRatio = 1;

    void OnGUI() {
        if (webView == IntPtr.Zero || !visibility)
            return;
        switch (Event.current.type) {
        case EventType.MouseDown:
        case EventType.MouseUp:
            hasFocus = rect.Contains(Input.mousePosition);
            break;
        }
        switch (Event.current.type) {
        case EventType.MouseMove:
        case EventType.MouseDown:
        case EventType.MouseDrag:
        case EventType.MouseUp:
        case EventType.ScrollWheel:
            if (hasFocus) {
                Vector3 p;
                p.x = Input.mousePosition.x - rect.x;
                p.y = Input.mousePosition.y - rect.y;
                {
                    int mouseState = 0;
                    if (Input.GetButtonDown("Fire1")) {
                        mouseState = 1;
                    } else if (Input.GetButton("Fire1")) {
                        mouseState = 2;
                    } else if (Input.GetButtonUp("Fire1")) {
                        mouseState = 3;
                    }
                    //_CWebViewPlugin_SendMouseEvent(webView, (int)p.x, (int)p.y, Input.GetAxis("Mouse ScrollWheel"), mouseState);
                    _CWebViewPlugin_SendMouseEvent(webView, (int)p.x, (int)p.y, Input.mouseScrollDelta.y, mouseState);
                }
            }
            break;
        case EventType.Repaint:
            while (!string.IsNullOrEmpty(inputString)) {
                var keyChars = inputString.Substring(0, 1);
                var keyCode = (ushort)inputString[0];
                inputString = inputString.Substring(1);
                if (!string.IsNullOrEmpty(keyChars) || keyCode != 0) {
                    Vector3 p;
                    p.x = Input.mousePosition.x - rect.x;
                    p.y = Input.mousePosition.y - rect.y;
                    _CWebViewPlugin_SendKeyEvent(webView, (int)p.x, (int)p.y, keyChars, keyCode, 1);
                }
            }
            if (texture != null) {
                Matrix4x4 m = GUI.matrix;
                GUI.matrix
                    = Matrix4x4.TRS(
                        new Vector3(0, Screen.height, 0),
                        Quaternion.identity,
                        new Vector3(1, -1, 1));
                Graphics.DrawTexture(rect, texture);
                GUI.matrix = m;
            }
            break;
        }
    }
#endif
}
