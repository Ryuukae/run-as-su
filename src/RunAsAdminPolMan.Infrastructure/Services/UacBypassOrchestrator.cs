using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Orchestrates the UAC bypass workflow and enforces Local Privilege Escalation (LPE) security mitigations.
/// </summary>
public class UacBypassOrchestrator : IUacBypassOrchestrator
{
    private readonly ITaskSchedulerService _taskService;
    private readonly IShortcutService _shortcutService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<UacBypassOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UacBypassOrchestrator"/> class.
    /// </summary>
    public UacBypassOrchestrator(
        ITaskSchedulerService taskService,
        IShortcutService shortcutService,
        ISecurityService securityService,
        ILogger<UacBypassOrchestrator> logger)
    {
        _taskService = taskService;
        _shortcutService = shortcutService;
        _securityService = securityService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> GenerateUacBypassShortcutAsync(AppPolicy policy, string shortcutSaveDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policy.FilePath) || !File.Exists(policy.FilePath))
        {
            return Result.Fail(new Error("FILE_NOT_FOUND", "The specified executable file does not exist."));
        }

        // 1. Enforce LPE Security Mitigation (File & Directory ACL Check)
        var securityCheck = _securityService.VerifyPathSecurity(policy.FilePath);
        if (!securityCheck.IsSuccess)
        {
            _logger.LogWarning("LPE Security Check Failed for {FilePath}: {Message}", policy.FilePath, securityCheck.Error?.Message);
            return Result.Fail(securityCheck.Error!);
        }

        // 2. Synthesize Scheduled Task (pointing to Bootstrapper instead of target executable)
        string bootstrapperPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RunAsAdminPolMan.Bootstrapper.exe");

        var bootstrapperPolicy = new AppPolicy
        {
            FilePath = bootstrapperPath,
            ProductName = policy.ProductName,
            Arguments = $"\"{policy.FilePath}\""
        };

        var taskResult = await _taskService.CreateElevatedTaskAsync(bootstrapperPolicy, cancellationToken);
        if (!taskResult.IsSuccess)
        {
            return Result.Fail(taskResult.Error!);
        }
        string taskName = taskResult.Value;

        // 3. Synthesize .lnk Shortcut
        string safeProductName = string.IsNullOrWhiteSpace(policy.ProductName)
            ? Path.GetFileNameWithoutExtension(policy.FilePath)
            : string.Join("_", policy.ProductName.Split(Path.GetInvalidFileNameChars()));

        string shortcutPath = Path.Combine(shortcutSaveDirectory, $"{safeProductName}.lnk");

        string targetPath = "schtasks.exe";
        string arguments = $"/run /tn \"{taskName}\"";
        string iconLocation = $"{policy.FilePath}, 0"; // Force 0th icon extraction

        var shortcutResult = await _shortcutService.CreateShortcutAsync(shortcutPath, targetPath, arguments, iconLocation, cancellationToken);
        if (!shortcutResult.IsSuccess)
        {
            return Result.Fail(shortcutResult.Error!);
        }

        _logger.LogInformation("Successfully orchestrated UAC Bypass shortcut at {ShortcutPath}", shortcutPath);
        return Result.Success();
    }
}