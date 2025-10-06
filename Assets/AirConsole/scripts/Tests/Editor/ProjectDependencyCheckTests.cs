#if !DISABLE_AIRCONSOLE
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NDream.AirConsole.Editor.Tests {
    public class ProjectDependencyCheckTests {
        [TearDown]
        public void TearDown() {
            ProjectDependencyCheck.UnityVersionProvider = null;
            ProjectDependencyCheck.InvokeErrorOrLogOverride = null;
            SemVerCheck.ValidateUnityVersionMinimumOverride = null;
        }

        [TestCase("2022.3.99f1", 2022, 3, 62, 2)]
        [TestCase("6000.0.99f1", 6000, 0, 58, 2)]
        [TestCase("6000.1.20f5", 6000, 1, 17, 1)]
        [TestCase("6000.2.5f1", 6000, 2, 6, 2)]
        [TestCase("2030.1.5f1", 6000, 0, 58, 2)]
        public void ValidateUnityVersion_PassesExpectedRequiredVersion(string unityVersion, int expectedMajor, int expectedMinor,
            int expectedBuild, int expectedRevision) {
            Version? capturedRequired = null;
            string? capturedDetectedVersion = null;
            SemVerCheck.ValidateUnityVersionMinimumOverride = (required, detected) => {
                capturedRequired = required;
                capturedDetectedVersion = detected;
                return true;
            };
            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;

            ProjectDependencyCheck.ValidateUnityVersion();

            Assert.That(capturedDetectedVersion, Is.EqualTo(unityVersion));
            Assert.That(capturedRequired, Is.Not.Null);
            Assert.That(capturedRequired, Is.EqualTo(new Version(expectedMajor, expectedMinor, expectedBuild, expectedRevision)));
        }

        [Test]
        public void ValidateUnityVersion_WhenBelowMinimum_LogsWarning() {
            const string unityVersion = "6000.0.1f1";
            string expectedMessage = "For security (CVE-2025-59489), AirConsole requires at least Unity 6000.0.58f2";
            SemVerCheck.ValidateUnityVersionMinimumOverride = (required, detected) => {
                Assert.That(required, Is.EqualTo(new Version(6000, 0, 58, 2)));
                Assert.That(detected, Is.EqualTo(unityVersion));
                return false;
            };
            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;

            LogAssert.Expect(LogType.Warning, expectedMessage);

            ProjectDependencyCheck.ValidateUnityVersion(false);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ValidateUnityVersion_WhenBelowMinimumAndErrorRequested_InvokesErrorHandler() {
            const string unityVersion = "6000.1.5f1";
            Version? capturedRequired = null;
            const string expectedRequired = "6000.1.17f1";
            bool handlerInvoked = false;
            SemVerCheck.ValidateUnityVersionMinimumOverride = (required, detected) => {
                capturedRequired = required;
                Assert.That(detected, Is.EqualTo(unityVersion));
                return false;
            };
            ProjectDependencyCheck.UnityVersionProvider = () => unityVersion;
            ProjectDependencyCheck.InvokeErrorOrLogOverride = (message, title, shallError) => {
                handlerInvoked = true;
                Assert.That(shallError, Is.True);
                Assert.That(message, Is.EqualTo($"For security (CVE-2025-59489), AirConsole requires at least Unity {expectedRequired}"));
                Assert.That(title, Is.EqualTo($"Insecure version {unityVersion}"));
            };

            ProjectDependencyCheck.ValidateUnityVersion(true);

            Assert.That(handlerInvoked, Is.True);
            Assert.That(capturedRequired, Is.EqualTo(new Version(6000, 1, 17, 1)));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
