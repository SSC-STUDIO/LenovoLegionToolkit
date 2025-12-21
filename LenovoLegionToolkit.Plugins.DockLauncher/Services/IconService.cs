using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LenovoLegionToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Service for extracting high-resolution icons from executables
/// </summary>
public class IconService
{
    /// <summary>
    /// Extract high-resolution icon from executable
    /// </summary>
    public static ImageSource? GetHighResolutionIcon(string executablePath, int size = 256)
    {
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
            return null;

        try
        {
            // Use optimized method with high-quality scaling
            return ExtractIconFallback(executablePath, size);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error extracting high-res icon: {ex.Message}", ex);
            return null;
        }
    }

    private static ImageSource? ExtractIconFallback(string executablePath, int size)
    {
        try
        {
            var icon = Icon.ExtractAssociatedIcon(executablePath);
            if (icon == null)
                return null;

            try
            {
                // Create high-resolution bitmap from icon
                using (var sourceBitmap = icon.ToBitmap())
                {
                    // Resize to desired size with highest quality settings
                    using (var resized = new Bitmap(size, size))
                    {
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            // Use highest quality interpolation
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            
                            // Clear with transparent background
                            graphics.Clear(System.Drawing.Color.Transparent);
                            
                            // Draw with high quality - use source bitmap size for better quality
                            graphics.DrawImage(sourceBitmap, 0, 0, size, size);
                        }

                        // Convert to BitmapSource with high DPI
                        var hBitmap = resized.GetHbitmap();
                        try
                        {
                            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromWidthAndHeight(size, size));
                            
                            // Freeze for better performance and to prevent modifications
                            bitmapSource.Freeze();
                            
                            return bitmapSource;
                        }
                        finally
                        {
                            // Delete the bitmap handle
                            PInvoke.DeleteObject((HGDIOBJ)hBitmap);
                        }
                    }
                }
            }
            finally
            {
                icon?.Dispose();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in icon fallback: {ex.Message}", ex);
            return null;
        }
    }
}
