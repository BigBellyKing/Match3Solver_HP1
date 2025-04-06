// FILE: Match3Solver/UIFunctions.cs (FIXED Namespace Ambiguity)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls; // Keep for RichTextBox
using System.Windows.Documents; // Keep for TextRange etc.
using SWMedia = System.Windows.Media; // Alias System.Windows.Media types
using SWShapes = System.Windows.Shapes; // Alias System.Windows.Shapes
using SDColor = System.Drawing.Color; // Alias System.Drawing.Color
using SDPoint = System.Drawing.Point; // Alias for System.Drawing.Point
using SDFont = System.Drawing.Font;   // Alias for System.Drawing.Font
using SDFontStyle = System.Drawing.FontStyle; // Alias for System.Drawing.FontStyle
// Use an alias for the Capture overlay TextElement to make it cleaner
using OverlayTextElement = Capture.Hook.Common.TextElement;
using Capture.Hook.Common; // For IOverlayElement, Overlay
using System.Globalization; // For logging format
using System.Windows.Media.Imaging; // For BitmapImage etc. if URIImage2Stream is kept


namespace Match3Solver
{
    public class UIFunctions
    {
        // --- Calibration Constants (Based on 1024x768 reference) ---
        // These MUST match the actual game layout at the reference resolution.
        private const int TILE_CENTER_START_X = 73; // Base X coord for center of tile [0,0] at 1024px width
        private const int TILE_CENTER_START_Y = 125; // Base Y coord for center of tile [0,0] at 1024px width
        private const int TILE_OFFSET_X = 70;      // Pixel distance between tile centers horizontally at 1024px width
        private const int TILE_OFFSET_Y = 70;      // Pixel distance between tile centers vertically at 1024px width
        private const float ASSET_BASE_WIDTH = 1024.0f; // The resolution the above constants are based on
        // --- End Constants ---

        // --- Colors for UI Grid and potentially Overlay ---
        internal Dictionary<int, SWMedia.Color> myColors = new Dictionary<int, SWMedia.Color> {
            // HP1 Colors (ensure these are accurate)
            { 0, SWMedia.Color.FromRgb(0xE9, 0xD0, 0x6C) }, // Joy (Yellow/Gold Bell)
            { 1, SWMedia.Color.FromRgb(0x49, 0xBA, 0xC0) }, // Sentiment (Cyan Teardrop)
            { 2, SWMedia.Color.FromRgb(0xF5, 0x58, 0xB8) }, // Passion (Pink Heart)
            { 3, SWMedia.Color.FromRgb(0xC4, 0x76, 0x18) }, // Romance (Orange Lips?)
            { 4, SWMedia.Color.FromRgb(0x11, 0x5F, 0xA9) }, // Talent (Blue Star?)
            { 5, SWMedia.Color.FromRgb(0xE3, 0x4E, 0x3E) }, // Sexuality (Red Whip?)
            { 6, SWMedia.Color.FromRgb(0x84, 0xB2, 0x43) }, // Flirtation (Green Diamond?)
            { 7, SWMedia.Color.FromRgb(0xB9, 0x68, 0xD5) }, // Broken Heart (Purple) - Adjusted based on SolverUtils color
            { 9, SWMedia.Color.FromRgb(245, 245, 245) }     // Unknown Tile Color for UI Grid
        };

        private MainWindow parent;

        public UIFunctions(MainWindow parent)
        {
            this.parent = parent;
        }

        // --- Keep URIImage2Stream (might be needed if other image resources are used later) ---
        private MemoryStream URIImage2Stream(Uri path)
        {
            try
            {
                var bitmapImage = new BitmapImage(path);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                var stream = new MemoryStream();
                encoder.Save(stream);
                stream.Flush();
                return stream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Converting URI {path} to stream failed: {ex.Message}");
                return null;
            }
        }

        // --- Keep MediaColorToDrawingColor ---
        public SDColor MediaColorToDrawingColor(SWMedia.Color mediaColor)
        {
            return SDColor.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }

        // --- Text-Based parseMovementAndDraw Method ---
        public Capture.Hook.Common.Overlay parseMovementAndDraw(SolverInterface.Movement command, int cellColorIndex, int screenHeight, int screenWidth)
        {
            var culture = CultureInfo.InvariantCulture;
            Console.WriteLine($"--- parseMovementAndDraw START (Text Overlay) ---");
            Console.WriteLine($"Input: Move=[{command.yPos},{command.xPos}] {(command.isVertical ? "V" : "H")} Amt={command.amount}, TileIdx={cellColorIndex}, Screen={screenWidth}x{screenHeight}");

            List<Capture.Hook.Common.IOverlayElement> elements = new List<Capture.Hook.Common.IOverlayElement>();

            // 1. Calculate Scaling Factor
            float scale = (float)screenWidth / ASSET_BASE_WIDTH;
            Console.WriteLine($"Scale factor: {scale.ToString(culture)} (screenWidth={screenWidth} / baseWidth={ASSET_BASE_WIDTH})");

            // 2. Calculate Position of the STARTING tile's center in screen coordinates
            int baseX = TILE_CENTER_START_X + (command.xPos * TILE_OFFSET_X);
            int baseY = TILE_CENTER_START_Y + (command.yPos * TILE_OFFSET_Y);
            int screenX = (int)(baseX * scale);
            int screenY = (int)(baseY * scale);
            Console.WriteLine($"Base Center Pos ({baseX},{baseY}) -> Scaled Center Pos ({screenX},{screenY})");

            int textPosX = screenX - 18;
            int textPosY = screenY - 12;

            // 3. Format the Text String
            string directionSymbol;
            if (command.isVertical)
            {
                directionSymbol = command.amount > 0 ? "v" : "^";
            }
            else
            {
                directionSymbol = command.amount > 0 ? ">" : "<";
            }
            string moveText = $"[{command.yPos},{command.xPos}] {directionSymbol}{Math.Abs(command.amount)}";
            Console.WriteLine($"Formatted Text: \"{moveText}\" at ({textPosX},{textPosY})");

            // 4. Define Font and Color
            SDFont font = new SDFont("Arial", 18.0f, SDFontStyle.Bold);
            SDColor textColor = SDColor.Black;

            // 5. Create TextElement (Use alias or full name) - FIX LINE 147
            var textElement = new OverlayTextElement(font) // Using the alias defined at the top
            // Alternatively: var textElement = new Capture.Hook.Common.TextElement(font)
            {
                Location = new SDPoint(textPosX, textPosY),
                Text = moveText,
                Color = textColor,
                AntiAliased = true
            };
            elements.Add(textElement);

            Console.WriteLine($"Total Elements Added: {elements.Count}");
            Console.WriteLine($"--- parseMovementAndDraw END ---");

            // 6. Return the Overlay object containing the text element
            return new Capture.Hook.Common.Overlay { Elements = elements, Hidden = false };
        }
        // --- END Text-Based parseMovementAndDraw ---


        // --- Keep Methods for Main Window UI Grid ---
        public void initBoardDisplay()
        {
            if (parent == null) return;
            int xPos = 10, yPos = 10;
            if (parent.boardDisplay?.Length > 0 && parent.boardDisplay[0]?.Length > 0 && parent.boardDisplay[0][0] != null)
            {
                for (int prevY = 0; prevY < parent.boardDisplay.Length; prevY++)
                {
                    if (parent.boardDisplay[prevY] == null) continue;
                    for (int prevX = 0; prevX < parent.boardDisplay[prevY].Length; prevX++)
                    {
                        if (parent.boardDisplay[prevY][prevX] != null)
                        {
                            parent.mainGrid.Children.Remove(parent.boardDisplay[prevY][prevX]);
                        }
                    }
                }
            }
            parent.boardDisplay = new SWShapes.Rectangle[parent.length][];
            for (int y = 0; y < parent.length; y++)
            {
                parent.boardDisplay[y] = new SWShapes.Rectangle[parent.width];
                xPos = 10;
                for (int x = 0; x < parent.width; x++)
                {
                    SWMedia.Color c = myColors.ContainsKey(9) ? myColors[9] : SWMedia.Color.FromRgb(128, 128, 128);
                    if (parent.board?.Length > y && parent.board[y]?.Length > x)
                    {
                        int tv = parent.board[y][x] % 10;
                        if (myColors.ContainsKey(tv)) c = myColors[tv];
                    }
                    parent.boardDisplay[y][x] = createRectangle(xPos, yPos, c);
                    parent.mainGrid.Children.Add(parent.boardDisplay[y][x]);
                    xPos += parent.boxSize;
                }
                yPos += parent.boxSize;
            }
        }

        private SWShapes.Rectangle createRectangle(int xPos, int yPos, SWMedia.Color color)
        {
            int currentBoxSize = (parent != null) ? parent.boxSize : 30;
            SWShapes.Rectangle rect = new SWShapes.Rectangle();
            rect.Fill = new SWMedia.SolidColorBrush(color);
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.Margin = new Thickness(xPos, yPos, 0, 0);
            rect.Stroke = new SWMedia.SolidColorBrush(SWMedia.Color.FromRgb(0, 0, 0));
            rect.Height = currentBoxSize;
            rect.Width = currentBoxSize;
            return rect;
        }

        public void drawBoard(int[][] board)
        {
            if (parent == null || parent.boardDisplay == null) return;
            for (int y = 0; y < parent.length; y++)
            {
                if (parent.boardDisplay[y] == null) continue;
                for (int x = 0; x < parent.width; x++)
                {
                    if (parent.boardDisplay[y][x] != null)
                    {
                        int tv = 9;
                        if (board?.Length > y && board[y]?.Length > x)
                        {
                            tv = board[y][x] % 10;
                        }
                        parent.boardDisplay[y][x].Fill = new SWMedia.SolidColorBrush(myColors.ContainsKey(tv) ? myColors[tv] : SWMedia.Color.FromRgb(128, 128, 128));
                    }
                }
            }
        }

        // Method to highlight text in RichTextBoxes (likely for sorting mode display)
        public void highLightMode(String s, RichTextBox r, RichTextBox o)
        {
            // Reset other RichTextBox
            var t2 = new TextRange(o.Document.ContentStart, o.Document.ContentEnd);
            // Use fully qualified name for WPF TextElement properties - FIX LINE 249
            t2.ApplyPropertyValue(System.Windows.Documents.TextElement.FontWeightProperty, FontWeights.Normal);
            // FIX LINE 250
            t2.ApplyPropertyValue(System.Windows.Documents.TextElement.ForegroundProperty, SWMedia.Brushes.Black);

            // Reset and highlight target RichTextBox
            var t = new TextRange(r.Document.ContentStart, r.Document.ContentEnd);
            var c = t.Start.GetInsertionPosition(LogicalDirection.Forward);
            // FIX LINE 255
            t.ApplyPropertyValue(System.Windows.Documents.TextElement.FontWeightProperty, FontWeights.Normal);
            // FIX LINE 256
            t.ApplyPropertyValue(System.Windows.Documents.TextElement.ForegroundProperty, SWMedia.Brushes.Black);

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
                        // FIX LINE 270
                        sel.ApplyPropertyValue(System.Windows.Documents.TextElement.FontWeightProperty, FontWeights.Bold);
                        // FIX LINE 271
                        sel.ApplyPropertyValue(System.Windows.Documents.TextElement.ForegroundProperty, SWMedia.Brushes.Red);
                        break;
                    }
                }
                c = c.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
        // --- End Main Window UI Grid Methods ---

    } // End Class UIFunctions
} // End Namespace Match3Solver