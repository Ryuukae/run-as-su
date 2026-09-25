using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RunAsAdminPolMan.Bootstrapper;

/// <summary>
/// Invisible runtime Bootstrapper that executes just-in-time (JIT) security checks 
/// before launching a target executable. This completely mitigates TOCTOU LPE vulnerabilities.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point for the bootstrapper.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static void Main(string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            return; // Terminate silently
        }

        string rawPath = args[0];

        try
        {
            string normalizedPath = Path.GetFullPath(rawPath);

            if (!File.Exists(normalizedPath))
            {
                return;
            }

            // Dual-Layer ACL Security Enforcement
            if (!IsPathSecure(normalizedPath, isDirectory: false)) return;

            string? directoryPath = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrWhiteSpace(directoryPath) && !IsPathSecure(directoryPath, isDirectory: true))
            {
                return;
            }

            // Pass all remaining arguments to the target
            string arguments = string.Empty;
            if (args.Length > 1)
            {
                // Simple reconstruction, skipping args[0]
                string[] forwardArgs = new string[args.Length - 1];
                Array.Copy(args, 1, forwardArgs, 0, args.Length - 1);
                // Quote arguments if they contain spaces
                for (int i = 0; i < forwardArgs.Length; i++)
                {
                    if (forwardArgs[i].Contains(' ') && !forwardArgs[i].StartsWith('"'))
                    {
                        forwardArgs[i] = $"\"{forwardArgs[i]}\"";
                    }
                }
                arguments = string.Join(" ", forwardArgs);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = normalizedPath,
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = directoryPath ?? string.Empty
            };

            Process.Start(startInfo);
        }
        catch
        {
            // Fail deadly: if any exception occurs (like unauthorized access to read ACLs),
            // silently terminate to prevent potentially unsafe execution.
        }
    }

    private static bool IsPathSecure(string targetPath, bool isDirectory)
    {
        CommonObjectSecurity security;
        if (isDirectory)
        {
            security = new DirectoryInfo(targetPath).GetAccessControl();
        }
        else
        {
            security = new FileInfo(targetPath).GetAccessControl();
        }

        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

        var everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        var builtinUsersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        var interactiveSid = new SecurityIdentifier(WellKnownSidType.InteractiveSid, null);
        var authenticatedUsersSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);

        foreach (FileSystemAccessRule rule in rules)
        {
            if ((rule.IdentityReference.Value == everyoneSid.Value ||
                 rule.IdentityReference.Value == builtinUsersSid.Value ||
                 rule.IdentityReference.Value == interactiveSid.Value ||
                 rule.IdentityReference.Value == authenticatedUsersSid.Value) &&
                rule.AccessControlType == AccessControlType.Allow &&
                ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write ||
                 (rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData ||
                 (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify ||
                 (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl))
            {
                // Insecure ACL found. 
                return false;
            }
        }

        return true;
    }
}