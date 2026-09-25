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
    private readonly Mock<ILogger<UacBypassOrchestrator>> _mockLogger;
    private readonly UacBypassOrchestrator _orchestrator;

    public UacBypassOrchestratorTests()
    {
        _mockTaskService = new Mock<ITaskSchedulerService>();
        _mockShortcutService = new Mock<IShortcutService>();
        _mockLogger = new Mock<ILogger<UacBypassOrchestrator>>();
        _orchestrator = new UacBypassOrchestrator(_mockTaskService.Object, _mockShortcutService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateUacBypassShortcutAsync_WhenFileDoesNotExist_ReturnsFail()
    {
        // Arrange
        var policy = new AppSettings { DefaultShortcutLocation = "Fake" }; // Just needing a bad path
        var badPolicy = new AppPolicy { FilePath = "C:\\does_not_exist_xyz123.exe" };

        // Act
        var result = await _orchestrator.GenerateUacBypassShortcutAsync(badPolicy, "C:\\Desktop");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("FILE_NOT_FOUND");
    }
}