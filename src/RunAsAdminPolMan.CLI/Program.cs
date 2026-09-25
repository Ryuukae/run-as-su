using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RunAsAdminPolMan.Core.Models;
using RunAsAdminPolMan.Core.Services;
using RunAsAdminPolMan.Infrastructure.Services;

namespace RunAsAdminPolMan.CLI;

/// <summary>
/// The main entry point class for the CLI application.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point method for the CLI application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        // Build DI Host manually
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IRegistryService, RegistryService>();
                services.AddSingleton<ISecurityService, SecurityService>();
                services.AddSingleton<IAppMetadataService, AppMetadataService>();
                services.AddSingleton<IBackupService, BackupService>();
            })
            .Build();

        var rootCommand = new RootCommand("RunAsAdmin Policy Manager CLI");

        // --- Policy Commands ---
        var policyCommand = new Command("policy", "Manage RunAsAdmin registry policies");
        var enableCommand = new Command("enable", "Enable RunAsAdmin for an executable");
        var disableCommand = new Command("disable", "Disable RunAsAdmin for an executable");
        var listCommand = new Command("list", "List all configured policies");

        var pathArgument = new Argument<string>("path", "The full path to the executable");
        var scopeOption = new Option<PolicyScope>("--scope", () => PolicyScope.CurrentUser, "The registry scope (CurrentUser or LocalMachine)");

        enableCommand.AddArgument(pathArgument);
        enableCommand.AddOption(scopeOption);
        
        disableCommand.AddArgument(pathArgument);
        disableCommand.AddOption(scopeOption);

        listCommand.AddOption(scopeOption);

        policyCommand.AddCommand(enableCommand);
        policyCommand.AddCommand(disableCommand);
        policyCommand.AddCommand(listCommand);

        // --- Backup Commands ---
        var backupCommand = new Command("backup", "Manage registry policy snapshots");
        var createBackupCommand = new Command("create", "Create a new backup snapshot");
        var restoreBackupCommand = new Command("restore", "Restore policies from a backup snapshot");
        
        var backupFileArgument = new Argument<string>("file", "The full path to the backup file");

        createBackupCommand.AddOption(scopeOption);
        restoreBackupCommand.AddArgument(backupFileArgument);

        backupCommand.AddCommand(createBackupCommand);
        backupCommand.AddCommand(restoreBackupCommand);

        rootCommand.AddCommand(policyCommand);
        rootCommand.AddCommand(backupCommand);

        // --- Handlers ---
        enableCommand.SetHandler(async (path, scope) => 
        {
            var absolutePath = Path.GetFullPath(path);
            var registry = host.Services.GetRequiredService<IRegistryService>();
            var result = await registry.SetPolicyAsync(absolutePath, enable: true, scope);
            if (result.IsSuccess)
            {
                Console.WriteLine($"[SUCCESS] Enabled RunAsAdmin for: {absolutePath} ({scope})");
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] Failed to enable policy: {result.Error?.Message}");
                Environment.ExitCode = 1;
            }
        }, pathArgument, scopeOption);

        disableCommand.SetHandler(async (path, scope) => 
        {
            var absolutePath = Path.GetFullPath(path);
            var registry = host.Services.GetRequiredService<IRegistryService>();
            var result = await registry.SetPolicyAsync(absolutePath, enable: false, scope);
            if (result.IsSuccess)
            {
                Console.WriteLine($"[SUCCESS] Disabled RunAsAdmin for: {absolutePath} ({scope})");
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] Failed to disable policy: {result.Error?.Message}");
                Environment.ExitCode = 1;
            }
        }, pathArgument, scopeOption);

        listCommand.SetHandler(async (scope) => 
        {
            var registry = host.Services.GetRequiredService<IRegistryService>();
            var result = await registry.GetPoliciesAsync(scope);
            if (result.IsSuccess)
            {
                Console.WriteLine($"--- RunAsAdmin Policies ({scope}) ---");
                foreach(var policy in result.Value)
                {
                    Console.WriteLine($"- {policy.FilePath}");
                }
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] Failed to list policies: {result.Error?.Message}");
                Environment.ExitCode = 1;
            }
        }, scopeOption);

        createBackupCommand.SetHandler(async (scope) => 
        {
            var backupService = host.Services.GetRequiredService<IBackupService>();
            var result = await backupService.CreateBackupAsync(scope);
            if (result.IsSuccess)
            {
                Console.WriteLine($"[SUCCESS] Backup created successfully for scope: {scope}");
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] Failed to create backup: {result.Error?.Message}");
                Environment.ExitCode = 1;
            }
        }, scopeOption);

        restoreBackupCommand.SetHandler(async (file) => 
        {
            var backupService = host.Services.GetRequiredService<IBackupService>();
            var result = await backupService.RestoreBackupAsync(file);
            if (result.IsSuccess)
            {
                Console.WriteLine($"[SUCCESS] Backup restored successfully from: {file}");
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] Failed to restore backup: {result.Error?.Message}");
                Environment.ExitCode = 1;
            }
        }, backupFileArgument);

        await rootCommand.InvokeAsync(args);
    }
}
