using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace LenovoLegionToolkit.Lib.System.Driver;

/// <summary>
/// Concrete implementation of IDriverWrapper that uses Windows DeviceIoControl.
/// </summary>
public class DriverWrapper : IDriverWrapper
{
    private bool _disposed = false;

    public SafeFileHandle GetHandle(string driverPath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DriverWrapper));

        try
        {
            var handle = PInvoke.CreateFile(driverPath,
                (uint)FILE_ACCESS_RIGHTS.FILE_READ_DATA | (uint)FILE_ACCESS_RIGHTS.FILE_WRITE_DATA,
                FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                null,
                FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                null);

            if (handle.IsInvalid)
                throw new InvalidOperationException($"Failed to get handle for driver: {driverPath}");

            return handle;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error getting driver handle: {driverPath}", ex);
        }
    }

    public Task<TOut> SendCommandAsync<TIn, TOut>(SafeFileHandle handle, uint controlCode, TIn input)
        where TIn : struct
        where TOut : struct
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DriverWrapper));

        try
        {
            if (PInvokeExtensions.DeviceIoControl(handle, controlCode, input, out TOut output))
            {
                return Task.FromResult(output);
            }

            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"DeviceIoControl failed with error: {error}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error sending driver command: controlCode={controlCode}", ex);
        }
    }

    public Task<uint> SendCommandAsync(SafeFileHandle handle, uint controlCode, uint input)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DriverWrapper));

        try
        {
            if (PInvokeExtensions.DeviceIoControl(handle, controlCode, input, out uint output))
            {
                return Task.FromResult(output);
            }

            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"DeviceIoControl failed with error: {error}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error sending driver command: controlCode={controlCode}", ex);
        }
    }

    public bool IsAvailable(string driverPath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DriverWrapper));

        try
        {
            var handle = PInvoke.CreateFile(driverPath,
                (uint)FILE_ACCESS_RIGHTS.FILE_READ_DATA,
                FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                null,
                FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                null);

            var isAvailable = !handle.IsInvalid;

            handle.Dispose();

            return isAvailable;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}