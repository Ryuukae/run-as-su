using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the IRegistryService using Win32 APIs.
/// </summary>
public class RegistryService : IRegistryService
{
    private const string AppCompatRegistryKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string RunAsAdminFlag = "~ RUNASADMIN";

    private readonly ILogger<RegistryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryService"/> class.
    /// </summary>
    public RegistryService(ILogger<RegistryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<AppPolicy>>> GetPoliciesAsync(PolicyScope scope, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var policies = new List<AppPolicy>();
                RegistryKey baseKey = GetBaseKey(scope);
                using RegistryKey? appCompatKey = baseKey.OpenSubKey(AppCompatRegistryKey, writable: false);

                if (appCompatKey is null)
                {
                    _logger.LogInformation("AppCompatFlags key does not exist for scope {Scope}. Returning empty list.", scope);
                    return Result<IEnumerable<AppPolicy>>.Success(policies);
                }

                foreach (string valueName in appCompatKey.GetValueNames())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (appCompatKey.GetValue(valueName) is string valueData)
                    {
                        bool isEnabled = valueData.Contains("RUNASADMIN", StringComparison.OrdinalIgnoreCase);
                        
                        policies.Add(new AppPolicy
                        {
                            FilePath = valueName,
                            IsEnabled = isEnabled,
                            Scope = scope
                        });
                    }
                }

                return Result<IEnumerable<AppPolicy>>.Success(policies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read registry policies for scope {Scope}.", scope);
                return Result<IEnumerable<AppPolicy>>.Fail(new Error("REG_READ_ERROR", "Failed to read registry policies due to system error."));
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> SetPolicyAsync(string filePath, bool enable, PolicyScope scope, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result.Fail(new Error("INVALID_PATH", "The file path cannot be empty."));
            }

            if (!System.IO.Path.IsPathRooted(filePath))
            {
                return Result.Fail(new Error("INVALID_PATH", "The Windows Registry requires an absolute file path for AppCompat flags."));
            }

            try
            {
                RegistryKey baseKey = GetBaseKey(scope);
                using RegistryKey appCompatKey = baseKey.CreateSubKey(AppCompatRegistryKey, writable: true);

                if (enable)
                {
                    appCompatKey.SetValue(filePath, RunAsAdminFlag, RegistryValueKind.String);
                    _logger.LogInformation("Successfully enabled RunAsAdmin policy for {FilePath} in {Scope}", filePath, scope);
                }
                else
                {
                    appCompatKey.DeleteValue(filePath, throwOnMissingValue: false);
                    _logger.LogInformation("Successfully removed RunAsAdmin policy for {FilePath} in {Scope}", filePath, scope);
                }

                return Result.Success();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while attempting to set registry policy in {Scope}.", scope);
                return Result.Fail(new Error("UNAUTHORIZED", "Administrator privileges are required to modify this registry scope."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set registry policy in {Scope}.", scope);
                return Result.Fail(new Error("REG_WRITE_ERROR", "An unexpected error occurred while modifying the registry."));
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> RemovePolicyAsync(string filePath, PolicyScope scope, CancellationToken cancellationToken = default)
    {
        return await SetPolicyAsync(filePath, enable: false, scope, cancellationToken);
    }

    private static RegistryKey GetBaseKey(PolicyScope scope)
    {
        return scope switch
        {
            PolicyScope.CurrentUser => Registry.CurrentUser,
            PolicyScope.LocalMachine => Registry.LocalMachine,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Invalid policy scope.")
        };
    }
}
