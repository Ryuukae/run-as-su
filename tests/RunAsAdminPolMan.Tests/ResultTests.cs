using FluentAssertions;

using RunAsAdminPolMan.Core.Models;

using Xunit;

namespace RunAsAdminPolMan.Tests;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Fail_CreatesFailedResultWithError()
    {
        // Arrange
        var error = new Error("TEST_ERR", "Test Error Message");

        // Act
        var result = Result.Fail(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        result.Error.Code.Should().Be("TEST_ERR");
    }

    [Fact]
    public void Success_WithValue_CreatesSuccessfulResultWithValue()
    {
        // Act
        var result = Result<string>.Success("Payload");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Payload");
    }

    [Fact]
    public void Fail_WithValue_CreatesFailedResultWithoutValue()
    {
        // Arrange
        var error = new Error("TEST_ERR", "Test Error Message");

        // Act
        var result = Result<string>.Fail(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }
}