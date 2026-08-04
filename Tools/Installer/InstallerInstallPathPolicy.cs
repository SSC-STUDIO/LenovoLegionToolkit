using System;
using System.IO;

namespace UniversalDeviceToolkit.Installer;

internal static class InstallerInstallPathPolicy
{
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
}
