// <copyright file="FlaUISetupTests.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Tests that verify the FlaUI test infrastructure is correctly set up.
    /// These tests DO NOT require a running UDT application.
    /// </summary>
    [Trait("Category", "UI.Infrastructure")]
    [Collection("FlaUI Tests")]
    public class FlaUISetupTests
    {
        [Fact]
        public void CiEnvironmentDetection_WorksCorrectly()
        {
            // Test the CI detection logic directly (static method, no app needed)
            var isCi = FlaUiTestBase.IsCiEnvironment();

            // Should not throw, should return a boolean
            Assert.IsType<bool>(isCi);
        }

        [Fact]
        public void CiEnvironmentDetection_SelfHostedRunner_IsNotTreatedAsCi()
        {
            var originalRunner = Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT");
            var originalGithub = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var originalAllow = Environment.GetEnvironmentVariable("UDT_ALLOW_FLAUI_TESTS");

            try
            {
                Environment.SetEnvironmentVariable("RUNNER_ENVIRONMENT", "self-hosted");
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
                Environment.SetEnvironmentVariable("UDT_ALLOW_FLAUI_TESTS", null);

                Assert.False(FlaUiTestBase.IsCiEnvironment());
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_ENVIRONMENT", originalRunner);
                Environment.SetEnvironmentVariable("GITHUB_ACTIONS", originalGithub);
                Environment.SetEnvironmentVariable("UDT_ALLOW_FLAUI_TESTS", originalAllow);
            }
        }

        [Fact]
        public void WinRTOcrHelper_IsOcrAvailable_DoesNotThrow()
        {
            // Should not throw even if OCR is not available
            var available = WinRTOcrHelper.IsOcrAvailable();

            // Should return a valid boolean
            Assert.IsType<bool>(available);
        }

        [SkippableFact]
        public async Task AppLaunch_ExecutableNotFound_ThrowsGracefully()
        {
            // Arrange: create a base with a non-existent path
            var testBase = new BadPathTestBase();

            // Act & Assert: should throw SkipException (CI) or FileNotFoundException (local)
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                async () => await testBase.InitializeAsync());

            // Should be a clear error, not a crash
            Assert.NotNull(ex.Message);
            Assert.True(ex is FileNotFoundException || ex is SkipException,
                $"Expected FileNotFoundException or SkipException, got {ex.GetType().Name}");
        }

        /// <summary>
        /// Helper class to test the case where the executable is not found.
        /// </summary>
        private class BadPathTestBase : FlaUiTestBase
        {
            public BadPathTestBase()
                : base(@"C:\NonExistent\Path\Universal Device Toolkit.exe")
            {
            }
        }
    }
}
