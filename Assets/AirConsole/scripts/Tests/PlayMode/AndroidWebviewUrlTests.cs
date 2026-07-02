#if !DISABLE_AIRCONSOLE
using NUnit.Framework;

namespace NDream.AirConsole.PlayMode.Tests {
    /// <summary>
    /// Covers <see cref="AirConsole.BuildWebviewUrl"/>, the pure query-string builder behind the Android webview URL.
    /// Exercises the Android vs Web runtime paths deterministically without a device: the Android-only parameters
    /// (<c>bundle-version</c>, <c>androidAppVersion</c>) must appear only when <c>isAndroidRuntime</c> is true.
    /// </summary>
    public class AndroidWebviewUrlTests {
        private const string BaseUrl = "https://www.airconsole.com/";
        private const string ConnectionUrl = "?id=5";
        private const string AppVersion = "1.2.3";
        private const int BundleVersionCode = 42;
        private const string GameId = "com.example.game";
        private const string GameVersion = "7";
        private const string UnityVersion = "2022.3.62f1";

        private static string BuildAndroid() =>
            AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, true, BundleVersionCode, AppVersion, GameId,
                GameVersion, UnityVersion, true);

        private static string BuildWeb() =>
            AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, false, BundleVersionCode, AppVersion, GameId,
                GameVersion, UnityVersion, true);

        [Test]
        public void AndroidPath_AddsAndroidAppVersion() {
            StringAssert.Contains($"&androidAppVersion={AppVersion}", BuildAndroid());
        }

        [Test]
        public void AndroidPath_AddsBundleVersion() {
            StringAssert.Contains($"&bundle-version={BundleVersionCode}", BuildAndroid());
        }

        [Test]
        public void WebPath_OmitsAndroidAppVersion() {
            Assert.IsFalse(BuildWeb().Contains("androidAppVersion"),
                "Web runtime URL must not carry the Android-only androidAppVersion parameter.");
        }

        [Test]
        public void WebPath_OmitsBundleVersion() {
            Assert.IsFalse(BuildWeb().Contains("bundle-version"),
                "Web runtime URL must not carry the Android-only bundle-version parameter.");
        }

        [Test]
        public void SharedParams_PresentOnBothPaths([Values(true, false)] bool isAndroidRuntime) {
            string url = AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, isAndroidRuntime, BundleVersionCode,
                AppVersion, GameId, GameVersion, UnityVersion, true);

            StringAssert.Contains($"&game-id={GameId}", url);
            StringAssert.Contains($"&game-version={GameVersion}", url);
            StringAssert.Contains($"&unity-version={UnityVersion}", url);
            StringAssert.Contains("&supportsNativeGameSizing=true", url);
        }

        [Test]
        public void NativeGameSizing_ReflectsSupportFlag() {
            string supported = AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, true, BundleVersionCode, AppVersion,
                GameId, GameVersion, UnityVersion, true);
            string unsupported = AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, true, BundleVersionCode, AppVersion,
                GameId, GameVersion, UnityVersion, false);

            StringAssert.Contains("&supportsNativeGameSizing=true", supported);
            StringAssert.Contains("&supportsNativeGameSizing=false", unsupported);
        }

        [Test]
        public void QueryString_IsWellFormed([Values(true, false)] bool isAndroidRuntime) {
            string url = AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, isAndroidRuntime, BundleVersionCode,
                AppVersion, GameId, GameVersion, UnityVersion, true);

            Assert.AreEqual(BaseUrl.Length, url.IndexOf('?'), "URL must contain exactly the connection-url query start.");
            Assert.AreEqual(1, url.Split('?').Length - 1, "URL must contain exactly one '?'.");
            Assert.IsFalse(url.Contains("&&"), "URL must not contain empty parameters (&&).");
            Assert.IsFalse(url.Contains("=&"), "URL must not contain dangling parameters (=&).");
            Assert.IsFalse(url.EndsWith("="), "URL must not end with a dangling '='.");
        }

        [Test]
        public void AppVersion_IsSourceOfAndroidAppVersion_NotGameVersion() {
            // Guards against wiring androidAppVersion to the wrong field (e.g. game-version).
            string url = AirConsole.BuildWebviewUrl(BaseUrl, ConnectionUrl, true, BundleVersionCode, "9.9", GameId,
                "1", UnityVersion, true);

            StringAssert.Contains("&androidAppVersion=9.9", url);
            StringAssert.Contains("&game-version=1", url);
        }
    }
}
#endif
