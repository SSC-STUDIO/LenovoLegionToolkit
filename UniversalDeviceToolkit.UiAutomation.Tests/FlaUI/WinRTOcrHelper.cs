// <copyright file="WinRTOcrHelper.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Provides deterministic text and screenshot helpers for UI automation.
    /// OCR is intentionally unavailable until a real implementation is added.
    /// </summary>
    public static class WinRTOcrHelper
    {
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
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(
                    (int)rect.Left,
                    (int)rect.Top,
                    0,
                    0,
                    new Size((int)rect.Width, (int)rect.Height),
                    CopyPixelOperation.SourceCopy);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static Task<string[]> ExtractVisibleTextAsync(AutomationElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return Task.FromResult(ExtractTextFromElementTree(element).ToArray());
        }

        public static bool IsOcrAvailable() => false;

        private static List<string> ExtractTextFromElementTree(AutomationElement element, int depth = 0)
        {
            var texts = new List<string>();
            if (depth > 3)
            {
                return texts;
            }

            try
            {
                AddIfPresent(texts, element.Properties.Name.Value);
                AddIfPresent(texts, element.Properties.AutomationId.Value);
                AddIfPresent(texts, element.Properties.HelpText.Value);

                try
                {
                    var textPattern = element.Patterns.Text.Pattern;
                    var text = textPattern?.DocumentRange?.GetText(int.MaxValue);
                    AddIfPresent(texts, text);
                }
                catch
                {
                    // Text pattern support is optional across UIA providers.
                }

                foreach (var child in element.FindAllChildren())
                {
                    texts.AddRange(ExtractTextFromElementTree(child, depth + 1));
                }
            }
            catch
            {
                // UI elements can disappear while the tree is being inspected.
            }

            return texts;
        }

        private static void AddIfPresent(ICollection<string> target, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value);
            }
        }
    }
}
