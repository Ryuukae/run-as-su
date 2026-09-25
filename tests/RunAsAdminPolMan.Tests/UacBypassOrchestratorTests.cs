using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;
using RunAsAdminPolMan.Infrastructure.Services;

using Xunit;

namespace RunAsAdminPolMan.Tests;

public class UacBypassOrchestratorTests
{
    private readonly Mock<ITaskSchedulerService> _mockTaskService;
    private readonly Mock<IShortcutService> _mockShortcutService;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<ILogger<UacBypassOrchestrator>> _mockLogger;
    private readonly UacBypassOrchestrator _orchestrator;

    public UacBypassOrchestratorTests()
    {
        _mockTaskService = new Mock<ITaskSchedulerService>();
        _mockShortcutService = new Mock<IShortcutService>();
        _mockSecurityService = new Mock<ISecurityService>();
        _mockLogger = new Mock<ILogger<UacBypassOrchestrator>>();
        _orchestrator = new UacBypassOrchestrator(_mockTaskService.Object, _mockShortcutService.Object, _mockSecurityService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateUacBypassShortcutAsync_WhenFileDoesNotExist_ReturnsFail()
    {
        // Arrange
        var badPolicy = new AppPolicy { FilePath = "C:\\does_not_exist_xyz123.exe" };

        // Act
        var result = await _orchestrator.GenerateUacBypassShortcutAsync(badPolicy, "C:\\Desktop");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("FILE_NOT_FOUND");
    }

    [Fact]
    public async Task GenerateUacBypassShortcutAsync_WhenSecurityCheckFails_ReturnsFail()
    {
        // Arrange
        // Create a real temp file so File.Exists passes
        string tempFile = Path.GetTempFileName();
        var policy = new AppPolicy { FilePath = tempFile };

        _mockSecurityService
            .Setup(s => s.VerifyPathSecurity(tempFile))
            .Returns(Result.Fail(new Error("LPE_VULNERABILITY_DETECTED", "Insecure.")));

        // Act
        var result = await _orchestrator.GenerateUacBypassShortcutAsync(policy, "C:\\Desktop");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("LPE_VULNERABILITY_DETECTED");

        // Cleanup
        File.Delete(tempFile);
    }
}