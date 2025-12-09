/*
 * Copyright (C) 2024 AirConsole
 *
 * C# proxy class implementing CWebViewPluginCallback Java interface.
 * Enables direct synchronous callbacks from Java to C# without UnitySendMessage delay.
 */

using UnityEngine;
using System;

#if UNITY_ANDROID

/// <summary>
/// AndroidJavaProxy implementation for CWebViewPluginCallback.
/// Java calls methods on this class directly, which then invokes WebViewObject callbacks.
/// Thread-safe: callbacks may arrive from any thread and are routed appropriately.
/// </summary>
public class WebViewCallback : AndroidJavaProxy
{
    private readonly WebViewObject _webViewObject;

    public WebViewCallback(WebViewObject webViewObject)
        : base("net.gree.unitywebview.CWebViewPluginCallback")
    {
        _webViewObject = webViewObject ?? throw new ArgumentNullException(nameof(webViewObject));
    }

    /// <summary>
    /// Called when JavaScript invokes Unity.call().
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void call(string message)
    {
        _webViewObject.CallFromJS(message);
    }

    /// <summary>
    /// Called when a navigation error occurs.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onError(string error)
    {
        _webViewObject.CallOnError(error);
    }

    /// <summary>
    /// Called when an HTTP error response is received.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onHttpError(string statusCode)
    {
        _webViewObject.CallOnHttpError(statusCode);
    }

    /// <summary>
    /// Called when page navigation starts.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onStarted(string url)
    {
        _webViewObject.CallOnStarted(url);
    }

    /// <summary>
    /// Called when page navigation finishes.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onLoaded(string url)
    {
        _webViewObject.CallOnLoaded(url);
    }

    /// <summary>
    /// Called when a hooked URL pattern is matched.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onHooked(string url)
    {
        _webViewObject.CallOnHooked(url);
    }

    /// <summary>
    /// Called when audio focus state changes.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onAudioFocusChanged(string state)
    {
        _webViewObject.CallOnAudioFocusChanged(state);
    }

    /// <summary>
    /// Called when soft keyboard visibility changes.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onKeyboardVisible(string height)
    {
        _webViewObject.SetKeyboardVisible(height);
    }

    /// <summary>
    /// Called when file chooser permissions are needed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public void onRequestFileChooserPermissions()
    {
        _webViewObject.RequestFileChooserPermissions();
    }
}

#endif
