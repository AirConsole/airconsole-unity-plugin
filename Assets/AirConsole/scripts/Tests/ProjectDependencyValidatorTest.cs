using System.Collections.Generic;
using NDream.AirConsole.Editor;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NDream.AirConsole.Tests {
    public class ProjectDependencyValidatorTest {
        [Test]
        public void TestAndroidPackages() {
            List<string> packagesToCheck = new();
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackages);
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackagesAndroid);
            
            foreach (string package in packagesToCheck) {
                Assert.IsTrue(ProjectDependencyValidatorTestRunner.RejectionReasons.ContainsKey(package), $"Package {package} is not in the rejection reasons list");
            }
        }

        [Test]
        public void TestAndroidAutomotivePackages() {
            List<string> packagesToCheck = new();
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackages);
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackagesAndroid);
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackagesAndroidAutomotive);
            
            foreach (string package in packagesToCheck) {
                Assert.IsTrue(ProjectDependencyValidatorTestRunner.RejectionReasons.ContainsKey(package), $"Package {package} is not in the rejection reasons list");
            } 
        }

        [Test]
        public void TestWebGLPackages() {
            List<string> packagesToCheck = new();
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackages);
            packagesToCheck.AddRange(ProjectDependencyValidatorTestRunner.BlockedPackagesWebGL);
            
            foreach (string package in packagesToCheck) {
                Assert.IsTrue(ProjectDependencyValidatorTestRunner.RejectionReasons.ContainsKey(package), $"Package {package} is not in the rejection reasons list");
            } 
        }
        
        
        public class ProjectDependencyValidatorTestRunner : ProjectDependencyValidator {
            public static Dictionary<string, string> RejectionReasons => rejectionReasons;
            public static List<string> BlockedPackages => blockedPackages;
            public static List<string> BlockedPackagesAndroid => blockedPackagesAndroid;
            public static List<string> BlockedPackagesAndroidAutomotive => blockedPackagesAndroidAutomotive;
            public static List<string> BlockedPackagesWebGL => blockedPackagesWebGL;
                
        }
    }
}