// FILE: Match3Solver/UIFunctions.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media; // Use SWColor alias below
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SDColor = System.Drawing.Color; // Alias for System.Drawing.Color
using SWColor = System.Windows.Media.Color; // Alias for System.Windows.Media.Color

namespace Match3Solver
{
    public class UIFunctions
    {
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


        // parseMovementAndDraw: Needs overlay calibration later
        public Capture.Hook.Common.Overlay parseMovementAndDraw(SolverInterface.Movement command, int cellColor, int height, int width)
        {
            SDColor tint = SDColor.White; // System.Drawing.Color
            switch (cellColor)
            {
                case 0: tint = SDColor.Yellow; break;
                case 1: tint = SDColor.Cyan; break;
                case 2: tint = SDColor.HotPink; break;
                case 3: tint = SDColor.Orange; break;
                case 4: tint = SDColor.DodgerBlue; break;
                case 5: tint = SDColor.Red; break;
                case 6: tint = SDColor.LimeGreen; break;
                case 7: tint = SDColor.MediumPurple; break;
                default: tint = SDColor.Black; break;
            }

            // --- !!! Needs HP1 Calibration !!! ---
            int startX = (int)(width * 0.2961), startY = (int)(height * 0.12686), offset = (int)(0.04688 * width);
            int dirOffX = command.isVertical ? (int)(-1 * (width * 0.0078125)) : (int)(width * 0.015625); int dirOffY = command.isVertical ? ((command.amount > 0) ? (int)(width * 0.01302083) : (int)(width * 0.015625)) : (int)(-1 * (width * 0.0052083));
            int headOffX = command.isVertical ? 0 : ((command.amount > 0) ? (int)(-1 * (width * 0.049479167)) : (int)(width * 0.0018229167)); int headOffY = command.isVertical ? ((command.amount > 0) ? (int)(-1 * (width * 0.0489584)) : (int)(width * 0.002604167)) : ((command.amount > 0) ? (int)(width * 0.00078125) : 1);
            // --- End Placeholder ---

            List<Capture.Hook.Common.IOverlayElement> elem = new List<Capture.Hook.Common.IOverlayElement>();
            if (arrowTailV.ContainsKey(0) && arrowTailH.ContainsKey(0) && arrowHeadV.ContainsKey(0) && arrowHeadH.ContainsKey(0))
            {
                int i = command.amount > 0 ? command.amount - 1 : command.amount + 1; while (i != 0) { int xOff = command.isVertical ? 0 : (command.amount > 0 ? i - 1 : i); int yOff = command.isVertical ? (command.amount > 0 ? i - 1 : i) : 0; elem.Add(new Capture.Hook.Common.ImageElement() { Location = new System.Drawing.Point((startX + (offset * command.xPos)) + (offset * xOff) + dirOffX, (startY + (offset * command.yPos)) + (offset * yOff) + dirOffY), Image = command.isVertical ? arrowTailV[0] : arrowTailH[0], Scale = (float)(width / 3840.0), Tint = tint }); if (command.amount > 0) i--; else i++; }
                int finalX = command.isVertical ? command.xPos : command.xPos + command.amount; int finalY = command.isVertical ? command.yPos + command.amount : command.yPos; elem.Add(new Capture.Hook.Common.ImageElement() { Location = new System.Drawing.Point(startX + (offset * finalX) + dirOffX + headOffX, startY + (offset * finalY) + dirOffY + headOffY), Image = command.isVertical ? arrowHeadV[0] : arrowHeadH[0], Angle = (command.amount > 0 ? 3.14159f : 0.0f), Scale = (float)(width / 3840.0), Tint = tint });
            }
            else { Console.WriteLine("Warning: Arrow overlay assets not loaded."); }
            return new Capture.Hook.Common.Overlay { Elements = elem, Hidden = false };
        }

        // initBoardDisplay: Uses 7x8 dimensions from parent
        public void initBoardDisplay()
        {
            // Check if parent is null before accessing its properties
            if (parent == null) return;

            int xPos = 10, yPos = 10;
            // Clear previous grid elements if any
            if (parent.boardDisplay != null && parent.boardDisplay.Length > 0 && parent.boardDisplay[0] != null && parent.boardDisplay[0].Length > 0 && parent.boardDisplay[0][0] != null)
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
            // Re-initialize the array
            parent.boardDisplay = new Rectangle[parent.length][]; // Use parent's length (7)

            for (int y = 0; y < parent.length; y++)
            { // Loop 0-6
                parent.boardDisplay[y] = new Rectangle[parent.width]; // Use parent's width (8)
                xPos = 10;
                for (int x = 0; x < parent.width; x++)
                { // Loop 0-7
                    SWColor c = myColors.ContainsKey(9) ? myColors[9] : SWColor.FromRgb(128, 128, 128); // Default Gray
                    // Check parent.board initialization before accessing
                    if (parent.board != null && y < parent.board.Length && parent.board[y] != null && x < parent.board[y].Length)
                    {
                        int tv = parent.board[y][x] % 10; // Get base tile type
                        if (myColors.ContainsKey(tv)) c = myColors[tv];
                    }
                    parent.boardDisplay[y][x] = createRectangle(xPos, yPos, c);
                    parent.mainGrid.Children.Add(parent.boardDisplay[y][x]);
                    xPos += parent.boxSize;
                }
                yPos += parent.boxSize;
            }
        }

        // createRectangle: Uses SWColor, returns Shapes.Rectangle
        private Rectangle createRectangle(int xPos, int yPos, SWColor color)
        {
            // Check if parent is null before accessing boxSize
            int currentBoxSize = (parent != null) ? parent.boxSize : 30; // Default size if parent is null

            Rectangle rect = new Rectangle();
            rect.Fill = new SolidColorBrush(color);
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.Margin = new Thickness(xPos, yPos, 0, 0);
            rect.Stroke = new SolidColorBrush(SWColor.FromRgb(0, 0, 0)); // Black
            rect.Height = currentBoxSize;
            rect.Width = currentBoxSize;
            return rect;
        }

        // drawBoard: Uses 7x8 dimensions, uses myColors
        public void drawBoard(int[][] board)
        {
            // Check if parent is null
            if (parent == null || parent.boardDisplay == null) return;

            for (int y = 0; y < parent.length; y++)
            { // Loop 0-6
              // Check if row exists
                if (parent.boardDisplay[y] == null) continue;

                for (int x = 0; x < parent.width; x++)
                { // Loop 0-7
                  // Check if element exists
                    if (parent.boardDisplay[y][x] != null)
                    {
                        int tv = 9; // Default unknown
                        // Check board bounds carefully
                        if (board != null && y < board.Length && board[y] != null && x < board[y].Length)
                        {
                            tv = board[y][x] % 10;
                        }
                        parent.boardDisplay[y][x].Fill = new SolidColorBrush(myColors.ContainsKey(tv) ? myColors[tv] : SWColor.FromRgb(128, 128, 128)); // Use SWColor
                    }
                }
            }
        }

        // highLightMode: No changes needed
        public void highLightMode(String s, RichTextBox r, RichTextBox o) { var t2 = new TextRange(o.Document.ContentStart, o.Document.ContentEnd); t2.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal); t2.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black); var t = new TextRange(r.Document.ContentStart, r.Document.ContentEnd); var c = t.Start.GetInsertionPosition(LogicalDirection.Forward); t.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal); t.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black); while (c != null) { string tr = c.GetTextInRun(LogicalDirection.Forward); if (!string.IsNullOrWhiteSpace(tr)) { int i = tr.IndexOf(s); if (i != -1) { var ss = c.GetPositionAtOffset(i, LogicalDirection.Forward); var se = ss.GetPositionAtOffset(s.Length, LogicalDirection.Forward); var sel = new TextRange(ss, se); sel.Text = s; sel.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold); sel.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red); r.Selection.Select(sel.Start, sel.End); r.Focus(); } } c = c.GetNextContextPosition(LogicalDirection.Forward); } }

    } // End Class
} // End Namespace