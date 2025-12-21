using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LenovoLegionToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Service for capturing window thumbnails using DWM API
/// </summary>
public class WindowThumbnailService
{
    private readonly Dictionary<nint, nint> _thumbnailCache = new();
    private readonly Dictionary<nint, ImageSource> _thumbnailImageCache = new();
    private readonly object _cacheLock = new();

    /// <summary>
    /// Get thumbnail for a window
    /// </summary>
    public ImageSource? GetWindowThumbnail(nint windowHandle, int width = 200, int height = 150)
    {
        if (windowHandle == IntPtr.Zero)
            return null;

        try
        {
            lock (_cacheLock)
            {
                // Check cache first
                if (_thumbnailImageCache.TryGetValue(windowHandle, out var cachedImage))
                {
                    return cachedImage;
                }

                // Get thumbnail using DWM API
                var thumbnail = CreateThumbnail(windowHandle, width, height);
                if (thumbnail != null)
                {
                    _thumbnailImageCache[windowHandle] = thumbnail;
                    return thumbnail;
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error getting window thumbnail: {ex.Message}", ex);
        }

        return null;
    }

    /// <summary>
    /// Get thumbnails for multiple windows
    /// </summary>
    public List<WindowThumbnailInfo> GetWindowThumbnails(List<nint> windowHandles, int width = 200, int height = 150)
    {
        var thumbnails = new List<WindowThumbnailInfo>();

        foreach (var handle in windowHandles)
        {
            try
            {
                var thumbnail = GetWindowThumbnail(handle, width, height);
                if (thumbnail != null)
                {
                    thumbnails.Add(new WindowThumbnailInfo
                    {
                        WindowHandle = handle,
                        Thumbnail = thumbnail
                    });
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error getting thumbnail for window {handle}: {ex.Message}", ex);
            }
        }

        return thumbnails;
    }

    private ImageSource? CreateThumbnail(nint windowHandle, int width, int height)
    {
        try
        {
            // Use DWM to get window thumbnail
            var sourceHwnd = (HWND)windowHandle;
            var destinationHwnd = (HWND)IntPtr.Zero; // We'll use a temporary window
            
            // For now, use a simpler approach: capture window using PrintWindow
            // DWM thumbnail requires a destination window, which is more complex
            return CaptureWindowBitmap(windowHandle, width, height);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error creating thumbnail: {ex.Message}", ex);
            return null;
        }
    }

    private ImageSource? CaptureWindowBitmap(nint windowHandle, int width, int height)
    {
        try
        {
            // Get window rectangle
            Windows.Win32.Foundation.RECT rect;
            if (!PInvoke.GetWindowRect((HWND)windowHandle, out rect))
                return null;

            var windowWidth = rect.right - rect.left;
            var windowHeight = rect.bottom - rect.top;

            if (windowWidth <= 0 || windowHeight <= 0)
                return null;

            // Calculate aspect ratio
            var aspectRatio = (double)windowWidth / windowHeight;
            int captureWidth, captureHeight;

            if (aspectRatio > (double)width / height)
            {
                captureWidth = width;
                captureHeight = (int)(width / aspectRatio);
            }
            else
            {
                captureHeight = height;
                captureWidth = (int)(height * aspectRatio);
            }

            // Use BitBlt to capture window
            var hdcSrc = PInvoke.GetWindowDC((HWND)windowHandle);
            if (hdcSrc == IntPtr.Zero)
                return null;

            try
            {
                var hdcDest = PInvoke.CreateCompatibleDC(hdcSrc);
                if (hdcDest == IntPtr.Zero)
                    return null;

                try
                {
                    var hBitmap = PInvoke.CreateCompatibleBitmap(hdcSrc, captureWidth, captureHeight);
                    if (hBitmap == IntPtr.Zero)
                        return null;

                    try
                    {
                        var hOld = PInvoke.SelectObject(hdcDest, (HGDIOBJ)hBitmap);
                        PInvoke.BitBlt(hdcDest, 0, 0, captureWidth, captureHeight, hdcSrc, 0, 0, Windows.Win32.Graphics.Gdi.ROP_CODE.SRCCOPY);
                        PInvoke.SelectObject(hdcDest, hOld);

                        // Convert to ImageSource
                        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(captureWidth, captureHeight));

                        bitmapSource.Freeze();
                        return bitmapSource;
                    }
                    finally
                    {
                        PInvoke.DeleteObject(hBitmap);
                    }
                }
                finally
                {
                    PInvoke.DeleteDC(hdcDest);
                }
            }
            finally
            {
                PInvoke.ReleaseDC((HWND)windowHandle, hdcSrc);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error capturing window bitmap: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Clear thumbnail cache
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _thumbnailCache.Clear();
            _thumbnailImageCache.Clear();
        }
    }

    /// <summary>
    /// Remove thumbnail from cache
    /// </summary>
    public void RemoveThumbnail(nint windowHandle)
    {
        lock (_cacheLock)
        {
            if (_thumbnailCache.TryGetValue(windowHandle, out var thumbnail))
            {
                try
                {
                    // Note: DWM thumbnail unregistration would require HTHUMBNAIL type
                    // For now, we just remove from cache
                }
                catch
                {
                    // Ignore errors
                }
                _thumbnailCache.Remove(windowHandle);
            }
            _thumbnailImageCache.Remove(windowHandle);
        }
    }
}

/// <summary>
/// Window thumbnail information
/// </summary>
public class WindowThumbnailInfo
{
    public nint WindowHandle { get; set; }
    public ImageSource? Thumbnail { get; set; }
    public string? WindowTitle { get; set; }
}


