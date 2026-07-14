// <copyright file="WinRTOcrHelper.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Provides text extraction capabilities for UI automation elements.
    ///
    /// Two strategies:
    ///   1. Native FlaUI element tree inspection (fast, reliable, always available)
    ///   2. WinRT OCR on a screenshot (slower, requires Windows SDK, better for visual text)
    ///
    /// Use <see cref="ExtractVisibleTextAsync"/> for the best-effort combined approach.
    /// </summary>
    public static class WinRTOcrHelper
    {
        private static readonly object _initLock = new();
        private static bool _isInitialized;
        private static bool _ocrAvailable;

        /// <summary>
        /// Captures a screenshot of the given automation element.
        /// </summary>
        public static Bitmap? CaptureElement(AutomationElement element)
        {
            try
            {
                var rect = element.Properties.BoundingRectangle.Value;
                if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                {
                    return null;
                }

                var bitmap = new Bitmap((int)rect.Width, (int)rect.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        (int)rect.Left,
                        (int)rect.Top,
                        0,
                        0,
                        new System.Drawing.Size((int)rect.Width, (int)rect.Height),
                        CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts visible text from the given automation element.
        /// First tries native element tree inspection (fast), then WinRT OCR (if available).
        /// </summary>
        public static async Task<string[]> ExtractVisibleTextAsync(AutomationElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            // Strategy 1: Native element tree — extract Name / AutomationId / HelpText
            var nativeTexts = ExtractTextFromElementTree(element);
            if (nativeTexts.Count > 0)
            {
                return nativeTexts.ToArray();
            }

            // Strategy 2: WinRT OCR on screenshot
            using var bitmap = CaptureElement(element);
            if (bitmap != null)
            {
                var ocrTexts = await ExtractTextFromBitmapAsync(bitmap);
                if (ocrTexts.Length > 0)
                {
                    return ocrTexts;
                }
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Extracts text from the automation element tree by reading standard properties.
        /// This is fast and doesn't require OCR.
        /// Limits recursion depth to avoid noise and duplicates.
        /// </summary>
        private static List<string> ExtractTextFromElementTree(AutomationElement element, int depth = 0)
        {
            var texts = new List<string>();

            if (depth > 3)
            {
                return texts;
            }

            try
            {
                // Get text from the element itself
                var name = element.Properties.Name.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    texts.Add(name);
                }

                var automationId = element.Properties.AutomationId.Value;
                if (!string.IsNullOrWhiteSpace(automationId))
                {
                    texts.Add(automationId);
                }

                var helpText = element.Properties.HelpText.Value;
                if (!string.IsNullOrWhiteSpace(helpText))
                {
                    texts.Add(helpText);
                }

                // Also check for text pattern
                try
                {
                    var textPattern = element.Patterns.Text.Pattern;
                    if (textPattern != null)
                    {
                        var range = textPattern.DocumentRange;
                        var text = range?.GetText(int.MaxValue);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            texts.Add(text);
                        }
                    }
                }
                catch
                {
                    // Text pattern not supported — that's fine
                }

                // Recursively get text from immediate children (not too deep to avoid noise)
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    var childTexts = ExtractTextFromElementTree(child, depth + 1);
                    texts.AddRange(childTexts);
                }
            }
            catch
            {
                // Best effort
            }

            return texts;
        }

        /// <summary>
        /// Returns true if WinRT OCR engine is available on this machine.
        /// </summary>
        public static bool IsOcrAvailable()
        {
            EnsureInitialized();
            return _ocrAvailable;
        }

        /// <summary>
        /// Extracts text from a bitmap using WinRT OCR.
        /// Returns empty array if OCR is unavailable.
        /// </summary>
        private static async Task<string[]> ExtractTextFromBitmapAsync(Bitmap bitmap)
        {
            EnsureInitialized();

            if (!_ocrAvailable)
            {
                return Array.Empty<string>();
            }

            try
            {
                return await RunWinRTOcrAsync(bitmap);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    // Check if WinRT OCR types are available
                    var ocrType = Type.GetType(
                        "Windows.Media.Ocr.OcrEngine, Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null");
                    _ocrAvailable = ocrType != null;
                }
                catch
                {
                    _ocrAvailable = false;
                }
                finally
                {
                    _isInitialized = true;
                }
            }
        }

        /// <summary>
        /// Runs WinRT OCR on the given bitmap.
        /// This is a best-effort implementation; the primary text extraction
        /// is done via the element tree (see <see cref="ExtractTextFromElementTree"/>).
        /// </summary>
        private static async Task<string[]> RunWinRTOcrAsync(Bitmap bitmap)
        {
            // WinRT OCR via reflection is fragile and requires complex interop.
            // For now, return empty — the element tree inspection is the primary strategy.
            // TODO: Implement proper WinRT OCR via the net10.0-windows TFM's built-in support
            // by adding a separate project that directly references WinRT APIs.
            return Array.Empty<string>();
        }
    }
}
