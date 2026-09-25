using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;
using RunAsAdminPolMan.ViewModels;
using Xunit;

namespace RunAsAdminPolMan.Tests;

public class MainViewModelTests
{
    private readonly Mock<IRegistryService> _mockRegistryService;
    private readonly Mock<IAppMetadataService> _mockMetadataService;
    private readonly Mock<IFileDialogService> _mockFileDialogService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _mockRegistryService = new Mock<IRegistryService>();
        _mockMetadataService = new Mock<IAppMetadataService>();
        _mockFileDialogService = new Mock<IFileDialogService>();

        _viewModel = new MainViewModel(
            _mockRegistryService.Object,
            _mockMetadataService.Object,
            _mockFileDialogService.Object);
    }

    [Fact]
    public async Task LoadPoliciesCommand_OnSuccess_PopulatesPoliciesAndStatus()
    {
        // Arrange
        var mockPolicies = new List<AppPolicy>
        {
            new() { FilePath = "C:\\app1.exe", IsEnabled = true, Scope = PolicyScope.CurrentUser },
            new() { FilePath = "C:\\app2.exe", IsEnabled = false, Scope = PolicyScope.CurrentUser }
        };

        _mockRegistryService
            .Setup(s => s.GetPoliciesAsync(PolicyScope.CurrentUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AppPolicy>>.Success(mockPolicies));

        _mockMetadataService
            .Setup(s => s.ExtractMetadataAsync(It.IsAny<AppPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppPolicy p, CancellationToken _) => Result<AppPolicy>.Success(p with { ProductName = "MockProduct" }));

        // Act
        await _viewModel.LoadPoliciesCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Policies.Should().HaveCount(2);
        _viewModel.Policies[0].ProductName.Should().Be("MockProduct");
        _viewModel.StatusMessage.Should().Be("Loaded 2 policies.");
    }

    [Fact]
    public async Task AddPoliciesCommand_OnSuccess_ReloadsPolicies()
    {
        // Arrange
        var files = new[] { "C:\\newapp.exe" };
        
        _mockRegistryService
            .Setup(s => s.SetPolicyAsync(files[0], true, PolicyScope.CurrentUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockRegistryService
            .Setup(s => s.GetPoliciesAsync(PolicyScope.CurrentUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AppPolicy>>.Success(new List<AppPolicy>()));

        // Act
        await _viewModel.AddPoliciesCommand.ExecuteAsync(files);

        // Assert
        _mockRegistryService.Verify(s => s.SetPolicyAsync(files[0], true, PolicyScope.CurrentUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockRegistryService.Verify(s => s.GetPoliciesAsync(PolicyScope.CurrentUser, It.IsAny<CancellationToken>()), Times.Once);
        _viewModel.StatusMessage.Should().Be("Loaded 0 policies.");
    }

    [Fact]
    public async Task ToggleScopeCommand_TogglesScopeAndReloads()
    {
        // Arrange
        _viewModel.CurrentScope.Should().Be(PolicyScope.CurrentUser); // Default

        _mockRegistryService
            .Setup(s => s.GetPoliciesAsync(PolicyScope.LocalMachine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<AppPolicy>>.Success(new List<AppPolicy>()));

        // Act
        await _viewModel.ToggleScopeCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CurrentScope.Should().Be(PolicyScope.LocalMachine);
        _mockRegistryService.Verify(s => s.GetPoliciesAsync(PolicyScope.LocalMachine, It.IsAny<CancellationToken>()), Times.Once);
    }
}
