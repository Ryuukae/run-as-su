namespace RunAsAdminPolMan.Core.Models;

/// <summary>
/// Defines the registry hive scope for the policy.
/// </summary>
public enum PolicyScope
{
    /// <summary>
    /// HKEY_CURRENT_USER - Applies only to the current user (No admin rights required).
    /// </summary>
    CurrentUser,
    
    /// <summary>
    /// HKEY_LOCAL_MACHINE - Applies to all users (Requires Administrator privileges to modify).
    /// </summary>
    LocalMachine
}
