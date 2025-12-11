using System;

/// <summary>
/// Represents a queued event in the WebView system.
/// Enqueued from any thread, dequeued and processed on the Unity main thread.
/// </summary>
public class WebViewEvent
{
    /// <summary>
    /// Enumeration of all supported WebView event types.
    /// Matches the message prefixes used by the native bridge.
    /// </summary>
    public enum EventType
    {
        /// <summary>JavaScript message from the WebView (CallFromJS).</summary>
        Message,
        /// <summary>General error from the WebView (CallOnError).</summary>
        Error,
        /// <summary>HTTP error from the WebView (CallOnHttpError).</summary>
        HttpError,
        /// <summary>Navigation started (CallOnStarted).</summary>
        Started,
        /// <summary>Navigation completed (CallOnLoaded).</summary>
        Loaded,
        /// <summary>URL hook triggered (CallOnHooked).</summary>
        Hooked,
        /// <summary>Cookie information retrieved (CallOnCookies).</summary>
        Cookies,
        /// <summary>Audio focus state changed (CallOnAudioFocusChanged).</summary>
        AudioFocusChanged,
        /// <summary>Keyboard visibility changed (SetKeyboardVisible).</summary>
        KeyboardHeightChanged,
        /// <summary>File chooser permissions requested (RequestFileChooserPermissions).</summary>
        FileChooserPermissions,
        /// <summary>Unknown or unrecognized event type.</summary>
        Unknown
    }

    /// <summary>
    /// The type of this event.
    /// </summary>
    public EventType Type { get; private set; }

    /// <summary>
    /// String payload for the event (e.g., JS message, error text, URL).
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// Integer payload for numeric data (e.g., audio focus state, keyboard height in pixels).
    /// </summary>
    public int IntPayload { get; private set; }

    /// <summary>
    /// Float payload reserved for future use.
    /// </summary>
    public float FloatPayload { get; private set; }

    /// <summary>
    /// Timestamp when the event was created (UTC).
    /// Useful for debugging and metrics.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Private constructor. Use factory methods to create events.
    /// </summary>
    private WebViewEvent(EventType type, string payload = null, int intPayload = 0, float floatPayload = 0f)
    {
        Type = type;
        Payload = payload;
        IntPayload = intPayload;
        FloatPayload = floatPayload;
        Timestamp = DateTime.UtcNow;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a JavaScript message event.
    /// </summary>
    /// <param name="message">The message payload from JavaScript.</param>
    /// <returns>A new WebViewEvent of type Message.</returns>
    public static WebViewEvent Message(string message)
    {
        return new WebViewEvent(EventType.Message, message);
    }

    /// <summary>
    /// Creates an error event.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A new WebViewEvent of type Error.</returns>
    public static WebViewEvent Error(string error)
    {
        return new WebViewEvent(EventType.Error, error);
    }

    /// <summary>
    /// Creates an HTTP error event.
    /// </summary>
    /// <param name="error">The HTTP error details.</param>
    /// <returns>A new WebViewEvent of type HttpError.</returns>
    public static WebViewEvent HttpError(string error)
    {
        return new WebViewEvent(EventType.HttpError, error);
    }

    /// <summary>
    /// Creates a navigation started event.
    /// </summary>
    /// <param name="url">The URL that started loading.</param>
    /// <returns>A new WebViewEvent of type Started.</returns>
    public static WebViewEvent Started(string url)
    {
        return new WebViewEvent(EventType.Started, url);
    }

    /// <summary>
    /// Creates a navigation loaded event.
    /// </summary>
    /// <param name="url">The URL that finished loading.</param>
    /// <returns>A new WebViewEvent of type Loaded.</returns>
    public static WebViewEvent Loaded(string url)
    {
        return new WebViewEvent(EventType.Loaded, url);
    }

    /// <summary>
    /// Creates a URL hook event.
    /// </summary>
    /// <param name="url">The hooked URL.</param>
    /// <returns>A new WebViewEvent of type Hooked.</returns>
    public static WebViewEvent Hooked(string url)
    {
        return new WebViewEvent(EventType.Hooked, url);
    }

    /// <summary>
    /// Creates a cookies event.
    /// </summary>
    /// <param name="cookies">The cookie string.</param>
    /// <returns>A new WebViewEvent of type Cookies.</returns>
    public static WebViewEvent Cookies(string cookies)
    {
        return new WebViewEvent(EventType.Cookies, cookies);
    }

    /// <summary>
    /// Creates an audio focus changed event.
    /// </summary>
    /// <param name="state">The audio focus state string.</param>
    /// <returns>A new WebViewEvent of type AudioFocusChanged.</returns>
    public static WebViewEvent AudioFocusChanged(string state)
    {
        return new WebViewEvent(EventType.AudioFocusChanged, state);
    }

    /// <summary>
    /// Creates a keyboard height changed event.
    /// </summary>
    /// <param name="height">The keyboard height in pixels.</param>
    /// <returns>A new WebViewEvent of type KeyboardHeightChanged.</returns>
    public static WebViewEvent KeyboardHeightChanged(int height)
    {
        return new WebViewEvent(EventType.KeyboardHeightChanged, height.ToString(), height);
    }

    /// <summary>
    /// Creates a keyboard height changed event from string.
    /// </summary>
    /// <param name="heightString">The keyboard height as string.</param>
    /// <returns>A new WebViewEvent of type KeyboardHeightChanged.</returns>
    public static WebViewEvent KeyboardHeightChanged(string heightString)
    {
        int height = 0;
        int.TryParse(heightString, out height);
        return new WebViewEvent(EventType.KeyboardHeightChanged, heightString, height);
    }

    /// <summary>
    /// Creates a file chooser permissions request event.
    /// </summary>
    /// <returns>A new WebViewEvent of type FileChooserPermissions.</returns>
    public static WebViewEvent FileChooserPermissions()
    {
        return new WebViewEvent(EventType.FileChooserPermissions);
    }

    /// <summary>
    /// Creates an event from a native bridge message string.
    /// Parses the "Type:Payload" format used by the Android bridge.
    /// </summary>
    /// <param name="message">The raw message from the native bridge.</param>
    /// <returns>A new WebViewEvent, or null if the message format is invalid.</returns>
    public static WebViewEvent FromNativeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return null;

        var colonIndex = message.IndexOf(':');
        if (colonIndex == -1)
            return null;

        var type = message.Substring(0, colonIndex);
        var payload = colonIndex + 1 < message.Length ? message.Substring(colonIndex + 1) : string.Empty;

        switch (type)
        {
            case "CallFromJS":
                return Message(payload);
            case "CallOnError":
                return Error(payload);
            case "CallOnHttpError":
                return HttpError(payload);
            case "CallOnStarted":
                return Started(payload);
            case "CallOnLoaded":
                return Loaded(payload);
            case "CallOnHooked":
                return Hooked(payload);
            case "CallOnCookies":
                return Cookies(payload);
            case "CallOnAudioFocusChanged":
                return AudioFocusChanged(payload);
            case "SetKeyboardVisible":
                return KeyboardHeightChanged(payload);
            case "RequestFileChooserPermissions":
                return FileChooserPermissions();
            default:
                return new WebViewEvent(EventType.Unknown, message);
        }
    }

    #endregion

    /// <summary>
    /// Returns a string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        return $"WebViewEvent[{Type}]: {Payload ?? "(no payload)"} @ {Timestamp:HH:mm:ss.fff}";
    }
}
