using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace VisualRegression.Smoke;

internal sealed class WindowVideoRecorder : IDisposable
{
    private readonly Process _process;
    private bool _disposed;

    private WindowVideoRecorder(Process process, string fileName)
    {
        _process = process;
        FileName = fileName;
    }

    public string FileName { get; }

    public static WindowVideoRecorder Start(int windowHandle, string outputPath)
    {
        if (!GetWindowRect((IntPtr)windowHandle, out var rect))
            throw new InvalidOperationException($"Could not read window bounds for video capture: {windowHandle}.");

        var width = Math.Max(2, rect.Right - rect.Left);
        var height = Math.Max(2, rect.Bottom - rect.Top);
        if (width % 2 != 0)
            width--;
        if (height % 2 != 0)
            height--;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                Arguments = string.Join(' ',
                    "-hide_banner -loglevel error -y",
                    "-f gdigrab -framerate 30",
                    $"-offset_x {rect.Left} -offset_y {rect.Top}",
                    $"-video_size {width}x{height}",
                    "-draw_mouse 1 -i desktop",
                    "-c:v libx264 -preset ultrafast -pix_fmt yuv420p",
                    $"\"{outputPath}\"")
            }
        };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("FFmpeg did not start.");
        }
        catch (Exception ex)
        {
            process.Dispose();
            throw new InvalidOperationException(
                "Video capture requires FFmpeg available on PATH.", ex);
        }

        return new WindowVideoRecorder(process, outputPath);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            if (!_process.HasExited)
            {
                _process.StandardInput.WriteLine("q");
                _process.StandardInput.Flush();
                if (!_process.WaitForExit(5000))
                    _process.Kill(entireProcessTree: true);
            }
        }
        finally
        {
            _process.Dispose();
        }

        if (!File.Exists(FileName) || new FileInfo(FileName).Length == 0)
            throw new InvalidOperationException($"FFmpeg did not produce a video: {FileName}");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
