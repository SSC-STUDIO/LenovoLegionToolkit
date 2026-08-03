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
    [Collection(TestCollections.FlaUI)]
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
            using var runner = new EnvironmentVariableScope("RUNNER_ENVIRONMENT", "self-hosted");
            using var github = new EnvironmentVariableScope("GITHUB_ACTIONS", "true");
            using var allow = new EnvironmentVariableScope("UDT_ALLOW_FLAUI_TESTS", null);
            using var session = new EnvironmentVariableScope("SESSIONNAME", null);

            Assert.False(FlaUiTestBase.IsCiEnvironment());
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

            // Act & Assert: missing executable and missing desktop preconditions are failures.
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                async () => await testBase.InitializeAsync());

            // Should be a clear error, not a crash
            Assert.NotNull(ex.Message);
            Assert.True(ex is FileNotFoundException || ex is InvalidOperationException,
                $"Expected FileNotFoundException or InvalidOperationException, got {ex.GetType().Name}");
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
