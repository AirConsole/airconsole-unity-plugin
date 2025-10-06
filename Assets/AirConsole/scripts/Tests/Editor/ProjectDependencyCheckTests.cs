#if !DISABLE_AIRCONSOLE
using System;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools;

namespace NDream.AirConsole.Editor.Tests {
    public class ProjectDependencyCheckTests {
        [TearDown]
        public void TearDown() {
            ProjectDependencyCheck.UnityVersionProvider = null;
            ProjectDependencyCheck.InvokeErrorOrLogOverride = null;
        }

        [TestCase("2022.3.62f2")]
        [TestCase("2022.3.62f3")]
        [TestCase("2022.3.99f1")]
        [TestCase("6000.0.99f1")]
        [TestCase("6000.1.20f5")]
        [TestCase("6000.2.6f2")]
        [TestCase("6000.3.0f1")]
        [TestCase("6001.0.0f1")]
        public void ValidateUnityVersion_PassesExpectedRequiredVersion(string unityVersion) {
            bool handlerInvoked = false;

            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;
            ProjectDependencyCheck.InvokeErrorOrLogOverride = (_, _, _) => { handlerInvoked = true; };

            ProjectDependencyCheck.ValidateUnityVersion();


            Assert.That(handlerInvoked, Is.False);
        }

        [TestCase("6000.1.12f1")]
        public void ValidateUnityVersion_WhenBelowMinimum_LogsWarning(string unityVersion) {
            const string expectedRequired = "6000.1.17f1";
            string expectedMessage = $"For security (CVE-2025-59489), AirConsole requires at least Unity {expectedRequired}";

            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;

            LogAssert.Expect(LogType.Warning, expectedMessage);

            ProjectDependencyCheck.ValidateUnityVersion(false);

            LogAssert.NoUnexpectedReceived();
        }

        [TestCase("6000.3.0b4")]
        public void ValidateUnityVersion_WhenInBeta_ThrowsBuildFailed(string unityVersion) {
            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;

            Assert.Throws<BuildFailedException>(() => ProjectDependencyCheck.ValidateUnityVersion(false),
                "UnityEditor.Build.BuildFailedException : No valid Unity Application version found. Format should be <MAJOR>.<MINOR>.<BUILD>f<REVISION>");
        }

        [TestCase("2022.3.61f1", "2022.3.62f2")]
        [TestCase("2022.3.62f1", "2022.3.62f2")]
        [TestCase("6000.0.57f1", "6000.0.58f2")]
        [TestCase("6000.0.58f1", "6000.0.58f2")]
        [TestCase("6000.1.16f1", "6000.1.17f1")]
        [TestCase("6000.2.5f1", "6000.2.6f2")]
        public void ValidateUnityVersion_WhenBelowMinimumAndErrorRequested_InvokesErrorHandler(string unityVersion,
            string expectedRequired) {
            bool handlerInvoked = false;

            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;
            ProjectDependencyCheck.InvokeErrorOrLogOverride = (message, title, shallError) => {
                handlerInvoked = true;
                Assert.That(shallError, Is.True);
                Assert.That(message, Is.EqualTo($"For security (CVE-2025-59489), AirConsole requires at least Unity {expectedRequired}"));
                Assert.That(title, Is.EqualTo($"Insecure version {unityVersion}"));
            };

            ProjectDependencyCheck.ValidateUnityVersion(true);

            Assert.That(handlerInvoked, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
