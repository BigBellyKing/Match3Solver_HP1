// FILE: Match3Solver/UIFunctions.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows; // For WPF types like Thickness, VerticalAlignment etc.
using System.Windows.Controls; // For RichTextBox
using System.Windows.Documents; // For TextRange, TextPointer
using System.Windows.Media; // For Brushes, Color (SWColor alias)
using System.Windows.Media.Imaging;
// using System.Windows.Shapes; // Can remove if Rectangle is always qualified, or keep if other shapes used
using SDColor = System.Drawing.Color;
using SWColor = System.Windows.Media.Color;
// Need System.Drawing for Point and Rectangle used with ImageElement/Bitmap
using System.Drawing; // Now includes Point and Rectangle

namespace Match3Solver
{
    public class UIFunctions
    {
        private const int OVERLAY_START_X = 39 + (70 / 2);
        private const int OVERLAY_START_Y = 90 + (70 / 2);
        private const int OVERLAY_OFFSET_X = 70;
        private const int OVERLAY_OFFSET_Y = 70;
        private const int TAIL_OFFSET_X_RIGHT = 5;
        private const int TAIL_OFFSET_X_LEFT = -5;
        private const int TAIL_OFFSET_Y_UP = -5;
        private const int TAIL_OFFSET_Y_DOWN = 5;
        private const int TAIL_OFFSET_Y_HORIZONTAL = 0;
        private const int TAIL_OFFSET_X_VERTICAL = 0;
        private const int HEAD_OFFSET_X = 0;
        private const int HEAD_OFFSET_Y = 0;
        private const float ASSET_BASE_WIDTH = 1920.0f;

        // --- End Calibration ---
        // --- Made internal for SolverUtils access ---
        internal Dictionary<int, SWColor> myColors = new Dictionary<int, SWColor> {
            { 0, SWColor.FromRgb(0xE9, 0xD0, 0x6C) /*Joy*/ }, { 1, SWColor.FromRgb(0x49, 0xBA, 0xC0) /*Sentiment*/ },
            { 2, SWColor.FromRgb(0xF5, 0x58, 0xB8) /*Passion*/ }, { 3, SWColor.FromRgb(0xC4, 0x76, 0x18) /*Romance*/ },
            { 4, SWColor.FromRgb(0x11, 0x5F, 0xA9) /*Talent*/ }, { 5, SWColor.FromRgb(0xE3, 0x4E, 0x3E) /*Sexuality*/ },
            { 6, SWColor.FromRgb(0x84, 0xB2, 0x43) /*Flirtation*/ }, { 7, SWColor.FromRgb(0xB9, 0x68, 0xD5) /*Broken*/ },
            { 9, SWColor.FromRgb(245, 245, 245) /*Unknown/Empty (Off-white)*/ }
        };

        Dictionary<int, byte[]> arrowHeadV = new Dictionary<int, byte[]>();
        Dictionary<int, byte[]> arrowHeadH = new Dictionary<int, byte[]>();
        Dictionary<int, byte[]> arrowTailV = new Dictionary<int, byte[]>();
        Dictionary<int, byte[]> arrowTailH = new Dictionary<int, byte[]>();

        private MainWindow parent;

        // Constructor can accept null parent if only used for helper functions like color conversion
        public UIFunctions(MainWindow parent)
        {
            this.parent = parent; // Parent might be null if created just for helper access
            loadAssets();
        }

        private void loadAssets()
        {
            try
            {
                System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
                // Check if keys already exist before adding (prevents error if constructor called multiple times)
                if (!arrowHeadV.ContainsKey(0))
                    arrowHeadV.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bheartheadv.png"))), typeof(byte[])));
                if (!arrowHeadH.ContainsKey(0))
                    arrowHeadH.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bheartheadh.png"))), typeof(byte[])));
                if (!arrowTailV.ContainsKey(0))
                    arrowTailV.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bhearttailv.png"))), typeof(byte[])));
                if (!arrowTailH.ContainsKey(0))
                    arrowTailH.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bhearttailh.png"))), typeof(byte[])));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading overlay assets: {ex.Message}");
            }
        }

        private MemoryStream URIImage2Stream(Uri path)
        {
            var bitmapImage = new BitmapImage(path);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Flush();
            return stream;
        }

        // Helper Function to convert color types
        public SDColor MediaColorToDrawingColor(SWColor mediaColor)
        {
            return SDColor.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }

        // parseMovementAndDraw: Uses calibrated constants
        public Capture.Hook.Common.Overlay parseMovementAndDraw(SolverInterface.Movement command, int cellColorIndex, int height, int width)
        {
            // ... (This method should be fine after the previous fix for Point) ...
            SDColor tint = SDColor.White;
            if (myColors.ContainsKey(cellColorIndex)) { tint = MediaColorToDrawingColor(myColors[cellColorIndex]); } else { tint = myColors.ContainsKey(7) ? MediaColorToDrawingColor(myColors[7]) : SDColor.Black; }

            List<Capture.Hook.Common.IOverlayElement> elem = new List<Capture.Hook.Common.IOverlayElement>();
            float scale = (float)(width / ASSET_BASE_WIDTH);

            if (!arrowTailV.ContainsKey(0) || !arrowTailH.ContainsKey(0) || !arrowHeadV.ContainsKey(0) || !arrowHeadH.ContainsKey(0))
            { return new Capture.Hook.Common.Overlay { Elements = elem, Hidden = true }; }

            try
            {
                int i = command.amount > 0 ? 1 : -1;
                while ((command.amount > 0 && i < command.amount) || (command.amount < 0 && i > command.amount))
                {
                    int currentXOffsetPixels = 0; int currentYOffsetPixels = 0; int basePixelX = OVERLAY_START_X + (command.xPos * OVERLAY_OFFSET_X); int basePixelY = OVERLAY_START_Y + (command.yPos * OVERLAY_OFFSET_Y); int directionAdjustX = 0; int directionAdjustY = 0;
                    if (command.isVertical) { currentYOffsetPixels = i * OVERLAY_OFFSET_Y; directionAdjustX = TAIL_OFFSET_X_VERTICAL; directionAdjustY = (command.amount > 0) ? TAIL_OFFSET_Y_DOWN : TAIL_OFFSET_Y_UP; } else { currentXOffsetPixels = i * OVERLAY_OFFSET_X; directionAdjustX = (command.amount > 0) ? TAIL_OFFSET_X_RIGHT : TAIL_OFFSET_X_LEFT; directionAdjustY = TAIL_OFFSET_Y_HORIZONTAL; }
                    elem.Add(new Capture.Hook.Common.ImageElement() { Location = new System.Drawing.Point(basePixelX + currentXOffsetPixels + directionAdjustX, basePixelY + currentYOffsetPixels + directionAdjustY), Image = command.isVertical ? arrowTailV[0] : arrowTailH[0], Scale = scale, Tint = tint });
                    if (command.amount > 0) i++; else i--;
                }
                int finalBaseX = OVERLAY_START_X + ((command.isVertical ? command.xPos : command.xPos + command.amount) * OVERLAY_OFFSET_X); int finalBaseY = OVERLAY_START_Y + ((command.isVertical ? command.yPos + command.amount : command.yPos) * OVERLAY_OFFSET_Y);
                elem.Add(new Capture.Hook.Common.ImageElement() { Location = new System.Drawing.Point(finalBaseX + HEAD_OFFSET_X, finalBaseY + HEAD_OFFSET_Y), Image = command.isVertical ? arrowHeadV[0] : arrowHeadH[0], Angle = (command.amount > 0) ? 0.0f : 3.14159f, Scale = scale, Tint = tint });

            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] Exception during overlay element calculation: {ex.Message}"); return new Capture.Hook.Common.Overlay { Elements = new List<Capture.Hook.Common.IOverlayElement>(), Hidden = true }; }
            return new Capture.Hook.Common.Overlay { Elements = elem, Hidden = false };
        }
        public void initBoardDisplay()
        {
            if (parent == null) return;
            int xPos = 10, yPos = 10;
            // Clear previous grid elements if any
            if (parent.boardDisplay?.Length > 0 && parent.boardDisplay[0]?.Length > 0 && parent.boardDisplay[0][0] != null) { for (int prevY = 0; prevY < parent.boardDisplay.Length; prevY++) { if (parent.boardDisplay[prevY] == null) continue; for (int prevX = 0; prevX < parent.boardDisplay[prevY].Length; prevX++) { if (parent.boardDisplay[prevY][prevX] != null) { parent.mainGrid.Children.Remove(parent.boardDisplay[prevY][prevX]); } } } }

            // Re-initialize the array with explicitly qualified type
            parent.boardDisplay = new System.Windows.Shapes.Rectangle[parent.length][];

            for (int y = 0; y < parent.length; y++)
            {
                // Explicitly qualify type
                parent.boardDisplay[y] = new System.Windows.Shapes.Rectangle[parent.width];
                xPos = 10;
                for (int x = 0; x < parent.width; x++)
                {
                    SWColor c = myColors.ContainsKey(9) ? myColors[9] : SWColor.FromRgb(128, 128, 128);
                    if (parent.board?.Length > y && parent.board[y]?.Length > x) { int tv = parent.board[y][x] % 10; if (myColors.ContainsKey(tv)) c = myColors[tv]; }
                    // createRectangle returns the correct type now
                    parent.boardDisplay[y][x] = createRectangle(xPos, yPos, c);
                    parent.mainGrid.Children.Add(parent.boardDisplay[y][x]);
                    xPos += parent.boxSize;
                }
                yPos += parent.boxSize;
            }
        }

        // createRectangle: Return type MUST be System.Windows.Shapes.Rectangle for WPF UI
        private System.Windows.Shapes.Rectangle createRectangle(int xPos, int yPos, SWColor color)
        {
            int currentBoxSize = (parent != null) ? parent.boxSize : 30;

            // Explicitly qualify Rectangle type
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
            rect.Fill = new SolidColorBrush(color);
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.Margin = new Thickness(xPos, yPos, 0, 0);
            rect.Stroke = new SolidColorBrush(SWColor.FromRgb(0, 0, 0));
            rect.Height = currentBoxSize;
            rect.Width = currentBoxSize;
            return rect;
        }

        // drawBoard: Uses 7x8 dimensions, uses myColors
        public void drawBoard(int[][] board)
        {
            if (parent == null || parent.boardDisplay == null) return;

            for (int y = 0; y < parent.length; y++)
            {
                if (parent.boardDisplay[y] == null) continue;
                for (int x = 0; x < parent.width; x++)
                {
                    if (parent.boardDisplay[y][x] != null) // Accessing the WPF Rectangle
                    {
                        int tv = 9;
                        if (board?.Length > y && board[y]?.Length > x) { tv = board[y][x] % 10; }
                        // Assign SolidColorBrush to Fill property
                        parent.boardDisplay[y][x].Fill = new SolidColorBrush(myColors.ContainsKey(tv) ? myColors[tv] : SWColor.FromRgb(128, 128, 128));
                    }
                }
            }
        }

        // highLightMode: Needs System.Windows.Media.Brushes
        public void highLightMode(String s, RichTextBox r, RichTextBox o)
        {
            var t2 = new TextRange(o.Document.ContentStart, o.Document.ContentEnd);
            t2.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            // --- FIX: Explicitly use System.Windows.Media.Brushes ---
            t2.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Black);

            var t = new TextRange(r.Document.ContentStart, r.Document.ContentEnd);
            var c = t.Start.GetInsertionPosition(LogicalDirection.Forward);
            t.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            // --- FIX: Explicitly use System.Windows.Media.Brushes ---
            t.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Black);

            while (c != null)
            {
                string tr = c.GetTextInRun(LogicalDirection.Forward);
                if (!string.IsNullOrWhiteSpace(tr))
                {
                    int i = tr.IndexOf(s);
                    if (i != -1)
                    {
                        var ss = c.GetPositionAtOffset(i, LogicalDirection.Forward);
                        var se = ss.GetPositionAtOffset(s.Length, LogicalDirection.Forward);
                        var sel = new TextRange(ss, se);
                        sel.Text = s;
                        sel.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                        // --- FIX: Explicitly use System.Windows.Media.Brushes ---
                        sel.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Red);
                        r.Selection.Select(sel.Start, sel.End);
                        r.Focus();
                    }
                }
                c = c.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

    } // End Class
} // End Namespace