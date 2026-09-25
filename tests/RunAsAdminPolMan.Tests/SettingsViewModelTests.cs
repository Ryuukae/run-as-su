using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;
using RunAsAdminPolMan.ViewModels;

using Xunit;

namespace RunAsAdminPolMan.Tests;

public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<SettingsViewModel>> _mockLogger;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<SettingsViewModel>>();
        _viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoadSettingsAsync_OnSuccess_PopulatesLocation()
    {
        // Arrange
        var mockSettings = new AppSettings();
        _mockSettingsService
            .Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AppSettings>.Success(mockSettings));

        // Act
        await _viewModel.LoadSettingsAsync();

        // Assert
        _viewModel.StatusMessage.Should().Be("Settings loaded.");
    }

    [Fact]
    public async Task SaveSettingsAsync_OnSuccess_UpdatesStatus()
    {
        // Arrange
        _mockSettingsService
            .Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _viewModel.SaveSettingsAsync();

        // Assert
        _mockSettingsService.Verify(
            s => s.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _viewModel.StatusMessage.Should().Be("Settings saved successfully.");
    }
}