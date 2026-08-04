using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace UniversalDeviceToolkit.Installer;

internal static class InstallerInstallPathPolicy
{
    private const FileSystemRights InstalledFileReadRights =
        FileSystemRights.ReadAndExecute;

    private const FileSystemRights InstalledDirectoryReadRights =
        FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory;

    public static bool IsUnderProgramFiles(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
            return false;

        var normalizedInstallDirectory = Path.GetFullPath(installDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var programFilesDirectory = Path.GetFullPath(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (normalizedInstallDirectory.Equals(programFilesDirectory, StringComparison.OrdinalIgnoreCase))
            return false;

        var prefix = programFilesDirectory + Path.DirectorySeparatorChar;
        return normalizedInstallDirectory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    public static void Validate(string installDirectory)
    {
        if (!IsUnderProgramFiles(installDirectory))
        {
            throw new UnauthorizedAccessException(
                "Universal Device Toolkit must be installed below the protected Program Files directory.");
        }

        var directory = new DirectoryInfo(Path.GetFullPath(installDirectory));
        var programFilesDirectory = new DirectoryInfo(Path.GetFullPath(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));

        // A junction below Program Files could redirect extraction to a user-writable
        // location, so reject existing reparse-point ancestors before writing payloads.
        for (var current = directory; current is not null &&
             !current.FullName.Equals(programFilesDirectory.FullName, StringComparison.OrdinalIgnoreCase);
             current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new UnauthorizedAccessException(
                    "The installer path contains a reparse-point directory and cannot be trusted.");
            }
        }
    }

    public static void PrepareForInstall(string installDirectory)
    {
        Validate(installDirectory);
        Directory.CreateDirectory(installDirectory);
        EnsureNoReparsePoints(installDirectory);
        ApplyProtectedAcl(installDirectory);
    }

    public static void ValidateForUninstall(string installDirectory)
    {
        Validate(installDirectory);
        if (Directory.Exists(installDirectory))
            EnsureNoReparsePoints(installDirectory);
    }

    private static void EnsureNoReparsePoints(string rootDirectory)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootDirectory);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new UnauthorizedAccessException(
                        "The installer path contains a reparse-point entry and cannot be trusted.");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                    pendingDirectories.Push(path);
            }
        }
    }

    private static void ApplyProtectedAcl(string rootDirectory)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootDirectory);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();
            SetDirectoryAcl(directory);

            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new UnauthorizedAccessException(
                        "The installer path contains a reparse-point entry and cannot be trusted.");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                    pendingDirectories.Push(path);
                else
                    SetFileAcl(path);
            }
        }
    }

    private static void SetDirectoryAcl(string path)
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        AddFullControlRule(security, WellKnownSidType.LocalSystemSid);
        AddFullControlRule(security, WellKnownSidType.BuiltinAdministratorsSid);
        AddReadRule(security, WellKnownSidType.BuiltinUsersSid);
        new DirectoryInfo(path).SetAccessControl(security);
    }

    private static void SetFileAcl(string path)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        AddFullControlRule(security, WellKnownSidType.LocalSystemSid);
        AddFullControlRule(security, WellKnownSidType.BuiltinAdministratorsSid);
        AddReadRule(security, WellKnownSidType.BuiltinUsersSid);
        new FileInfo(path).SetAccessControl(security);
    }

    private static void AddFullControlRule(DirectorySecurity security, WellKnownSidType sidType)
    {
        var sid = new SecurityIdentifier(sidType, domainSid: null);
        security.AddAccessRule(new FileSystemAccessRule(
            sid,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
    }

    private static void AddFullControlRule(FileSecurity security, WellKnownSidType sidType)
    {
        var sid = new SecurityIdentifier(sidType, domainSid: null);
        security.AddAccessRule(new FileSystemAccessRule(
            sid,
            FileSystemRights.FullControl,
            InheritanceFlags.None,
            PropagationFlags.None,
            AccessControlType.Allow));
    }

    private static void AddReadRule(DirectorySecurity security, WellKnownSidType sidType)
    {
        var sid = new SecurityIdentifier(sidType, domainSid: null);
        security.AddAccessRule(new FileSystemAccessRule(
            sid,
            InstalledDirectoryReadRights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
    }

    private static void AddReadRule(FileSecurity security, WellKnownSidType sidType)
    {
        var sid = new SecurityIdentifier(sidType, domainSid: null);
        security.AddAccessRule(new FileSystemAccessRule(
            sid,
            InstalledFileReadRights,
            InheritanceFlags.None,
            PropagationFlags.None,
            AccessControlType.Allow));
    }
}
