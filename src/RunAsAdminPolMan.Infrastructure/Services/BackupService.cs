using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of the IBackupService using reg.exe.
/// </summary>
public class BackupService : IBackupService
{
    private const string RegistryPathHKCU = @"HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string RegistryPathHKLM = @"HKLM\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

    private readonly ILogger<BackupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    public BackupService(ILogger<BackupService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> CreateBackupAsync(PolicyScope scope, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                string registryPath = scope == PolicyScope.CurrentUser ? RegistryPathHKCU : RegistryPathHKLM;
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"RunAsAdmin_Backup_{scope}_{timestamp}.reg";

                // Store backups in AppData or a dedicated folder. For simplicity, we use Documents.
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string backupDir = Path.Combine(documentsPath, "RunAsAdmin Policy Manager", "Backups");

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string backupFilePath = Path.Combine(backupDir, fileName);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                processStartInfo.ArgumentList.Add("export");
                processStartInfo.ArgumentList.Add(registryPath);
                processStartInfo.ArgumentList.Add(backupFilePath);
                processStartInfo.ArgumentList.Add("/y");

                using var process = Process.Start(processStartInfo);
                process?.WaitForExit();

                if (process?.ExitCode != 0)
                {
                    _logger.LogWarning("reg.exe export failed with exit code {ExitCode}.", process?.ExitCode);
                    return Result<string>.Fail(new Error("BACKUP_FAILED", "Failed to generate the registry export file."));
                }

                _logger.LogInformation("Successfully created registry backup at {FilePath}", backupFilePath);
                return Result<string>.Success(backupFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create registry backup for scope {Scope}.", scope);
                return Result<string>.Fail(new Error("BACKUP_ERROR", "An unexpected error occurred while creating the backup."));
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                return Result.Fail(new Error("FILE_NOT_FOUND", "The specified backup file does not exist."));
            }

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                processStartInfo.ArgumentList.Add("import");
                processStartInfo.ArgumentList.Add(backupFilePath);

                using var process = Process.Start(processStartInfo);
                process?.WaitForExit();

                if (process?.ExitCode != 0)
                {
                    _logger.LogWarning("reg.exe import failed with exit code {ExitCode}.", process?.ExitCode);
                    return Result.Fail(new Error("RESTORE_FAILED", "Failed to restore the registry from the backup file."));
                }

                _logger.LogInformation("Successfully restored registry backup from {FilePath}", backupFilePath);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore registry backup from {FilePath}.", backupFilePath);
                return Result.Fail(new Error("RESTORE_ERROR", "An unexpected error occurred while restoring the backup."));
            }
        }, cancellationToken);
    }
}