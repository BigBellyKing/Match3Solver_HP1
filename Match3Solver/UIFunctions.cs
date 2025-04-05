// FILE: Match3Solver/UIFunctions.cs (Simplified Angle & Coordinate Test)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SDColor = System.Drawing.Color; // Alias System.Drawing.Color
using SWColor = System.Windows.Media.Color; // Alias System.Windows.Media.Color
using System.Drawing; // Includes Point
using System.Globalization; // For logging format

namespace Match3Solver
{
    public class UIFunctions
    {
        // --- Calibration Constants (1024x768) ---
        private const int TILE_CENTER_START_X = 73;
        private const int TILE_CENTER_START_Y = 125;
        private const int TILE_OFFSET_X = 70;
        private const int TILE_OFFSET_Y = 70;
        private const float ASSET_BASE_WIDTH = 1024.0f;
        // --- End Constants ---

        // --- Angle Constants ---
        private const float ANGLE_RIGHT = 0.0f;
        private const float ANGLE_DOWN = (float)(Math.PI / 2.0);
        private const float ANGLE_LEFT = (float)Math.PI;
        private const float ANGLE_UP = (float)(3.0 * Math.PI / 2.0);
        // --- End Constants ---

        // Colors, Assets, Parent, Constructor, LoadAssets, URIImage2Stream, MediaColorToDrawingColor...
        internal Dictionary<int, SWColor> myColors = new Dictionary<int, SWColor> { { 0, SWColor.FromRgb(0xE9, 0xD0, 0x6C) }, { 1, SWColor.FromRgb(0x49, 0xBA, 0xC0) }, { 2, SWColor.FromRgb(0xF5, 0x58, 0xB8) }, { 3, SWColor.FromRgb(0xC4, 0x76, 0x18) }, { 4, SWColor.FromRgb(0x11, 0x5F, 0xA9) }, { 5, SWColor.FromRgb(0xE3, 0x4E, 0x3E) }, { 6, SWColor.FromRgb(0x84, 0xB2, 0x43) }, { 7, SWColor.FromRgb(0xB9, 0x68, 0xD5) }, { 9, SWColor.FromRgb(245, 245, 245) } };
        Dictionary<int, byte[]> arrowHeadV = new Dictionary<int, byte[]>(); Dictionary<int, byte[]> arrowHeadH = new Dictionary<int, byte[]>(); Dictionary<int, byte[]> arrowTailV = new Dictionary<int, byte[]>(); Dictionary<int, byte[]> arrowTailH = new Dictionary<int, byte[]>();
        private MainWindow parent;
        public UIFunctions(MainWindow parent) { this.parent = parent; loadAssets(); }
        private void loadAssets() { try { System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter(); if (!arrowHeadV.ContainsKey(0)) arrowHeadV.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bheartheadv.png"))), typeof(byte[]))); if (!arrowHeadH.ContainsKey(0)) arrowHeadH.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bheartheadh.png"))), typeof(byte[]))); if (!arrowTailV.ContainsKey(0)) arrowTailV.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bhearttailv.png"))), typeof(byte[]))); if (!arrowTailH.ContainsKey(0)) arrowTailH.Add(0, (byte[])converter.ConvertTo(System.Drawing.Image.FromStream(URIImage2Stream(new Uri(@"pack://application:,,,/Resources/bhearttailh.png"))), typeof(byte[]))); } catch (Exception ex) { Console.WriteLine($"[ERROR] Loading overlay assets failed: {ex.Message}"); } }
        private MemoryStream URIImage2Stream(Uri path) { try { var bitmapImage = new BitmapImage(path); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmapImage)); var stream = new MemoryStream(); encoder.Save(stream); stream.Flush(); return stream; } catch (Exception ex) { Console.WriteLine($"[ERROR] Converting URI {path} to stream failed: {ex.Message}"); return null; } }
        public SDColor MediaColorToDrawingColor(SWColor mediaColor) { return SDColor.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B); }

        // --- SIMPLIFIED Angle & Coordinate Test ---
        public Capture.Hook.Common.Overlay parseMovementAndDraw(SolverInterface.Movement command, int cellColorIndex, int screenHeight, int screenWidth)
        {
            var culture = CultureInfo.InvariantCulture;
            Console.WriteLine($"--- parseMovementAndDraw START (Simplified Test) ---");
            Console.WriteLine($"Input: Move=[{command.yPos},{command.xPos}] {(command.isVertical ? "V" : "H")} Amt={command.amount}, TileIdx={cellColorIndex}, Screen={screenWidth}x{screenHeight}");

            List<Capture.Hook.Common.IOverlayElement> elements = new List<Capture.Hook.Common.IOverlayElement>();

            // Use HORIZONTAL assets ONLY for now
            if (arrowHeadH.Count == 0)
            {
                Console.WriteLine("[ERROR] Horizontal Head asset not loaded."); return new Capture.Hook.Common.Overlay { Elements = elements, Hidden = true };
            }
            byte[] headAsset = arrowHeadH[0];

            SDColor tint;
            if (myColors.ContainsKey(cellColorIndex)) { tint = MediaColorToDrawingColor(myColors[cellColorIndex]); }
            else { tint = myColors.ContainsKey(7) ? MediaColorToDrawingColor(myColors[7]) : SDColor.Black; Console.WriteLine($"[WARN] Invalid cellColorIndex '{cellColorIndex}'. Defaulting tint."); }

            float scale = (float)screenWidth / ASSET_BASE_WIDTH;

            // --- Revised Angle Calculation ---
            float headAngle = ANGLE_RIGHT; // Default to right
            string directionName = "Unknown";

            if (command.isVertical)
            {
                if (command.amount > 0) { headAngle = ANGLE_DOWN; directionName = "Down"; } // Down
                else { headAngle = ANGLE_UP; directionName = "Up"; } // Up
            }
            else
            {
                if (command.amount > 0) { headAngle = ANGLE_RIGHT; directionName = "Right"; } // Right
                else { headAngle = ANGLE_LEFT; directionName = "Left"; } // Left
            }
            Console.WriteLine($"Direction: {directionName}, Angle: {headAngle.ToString(culture)} rad ({Math.Round(headAngle * 180.0 / Math.PI)} deg)");
            // --- End Revised Angle Calculation ---

            // --- Simple Head Placement ONLY ---
            // Calculate center of the STARTING tile
            int startX = TILE_CENTER_START_X + (command.xPos * TILE_OFFSET_X);
            int startY = TILE_CENTER_START_Y + (command.yPos * TILE_OFFSET_Y);

            // Calculate center of the DESTINATION tile
            int destX = startX + (command.isVertical ? 0 : command.amount * TILE_OFFSET_X);
            int destY = startY + (command.isVertical ? command.amount * TILE_OFFSET_Y : 0);
            Console.WriteLine($"  Head Placement: Dest Tile Center=({destX}, {destY})");

            elements.Add(new Capture.Hook.Common.ImageElement()
            {
                Location = new System.Drawing.Point(destX, destY), // Place head directly at destination center
                Image = headAsset, // Use horizontal asset
                Scale = scale,
                Angle = headAngle, // Apply calculated angle
                Tint = tint
            });
            // --- End Simple Head Placement ---

            // Tails are disabled for this test

            Console.WriteLine($"Total Elements: {elements.Count} (Head Only)");
            Console.WriteLine($"--- parseMovementAndDraw END ---");
            return new Capture.Hook.Common.Overlay { Elements = elements, Hidden = false };
        }
        // --- END SIMPLIFIED Test ---


        // initBoardDisplay, createRectangle, drawBoard, highLightMode...
        public void initBoardDisplay() { if (parent == null) return; int xPos = 10, yPos = 10; if (parent.boardDisplay?.Length > 0 && parent.boardDisplay[0]?.Length > 0 && parent.boardDisplay[0][0] != null) { for (int prevY = 0; prevY < parent.boardDisplay.Length; prevY++) { if (parent.boardDisplay[prevY] == null) continue; for (int prevX = 0; prevX < parent.boardDisplay[prevY].Length; prevX++) { if (parent.boardDisplay[prevY][prevX] != null) { parent.mainGrid.Children.Remove(parent.boardDisplay[prevY][prevX]); } } } } parent.boardDisplay = new System.Windows.Shapes.Rectangle[parent.length][]; for (int y = 0; y < parent.length; y++) { parent.boardDisplay[y] = new System.Windows.Shapes.Rectangle[parent.width]; xPos = 10; for (int x = 0; x < parent.width; x++) { SWColor c = myColors.ContainsKey(9) ? myColors[9] : SWColor.FromRgb(128, 128, 128); if (parent.board?.Length > y && parent.board[y]?.Length > x) { int tv = parent.board[y][x] % 10; if (myColors.ContainsKey(tv)) c = myColors[tv]; } parent.boardDisplay[y][x] = createRectangle(xPos, yPos, c); parent.mainGrid.Children.Add(parent.boardDisplay[y][x]); xPos += parent.boxSize; } yPos += parent.boxSize; } }
        private System.Windows.Shapes.Rectangle createRectangle(int xPos, int yPos, SWColor color) { int currentBoxSize = (parent != null) ? parent.boxSize : 30; System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle(); rect.Fill = new SolidColorBrush(color); rect.VerticalAlignment = VerticalAlignment.Top; rect.HorizontalAlignment = HorizontalAlignment.Left; rect.Margin = new Thickness(xPos, yPos, 0, 0); rect.Stroke = new SolidColorBrush(SWColor.FromRgb(0, 0, 0)); rect.Height = currentBoxSize; rect.Width = currentBoxSize; return rect; }
        public void drawBoard(int[][] board) { if (parent == null || parent.boardDisplay == null) return; for (int y = 0; y < parent.length; y++) { if (parent.boardDisplay[y] == null) continue; for (int x = 0; x < parent.width; x++) { if (parent.boardDisplay[y][x] != null) { int tv = 9; if (board?.Length > y && board[y]?.Length > x) { tv = board[y][x] % 10; } parent.boardDisplay[y][x].Fill = new SolidColorBrush(myColors.ContainsKey(tv) ? myColors[tv] : SWColor.FromRgb(128, 128, 128)); } } } }
        public void highLightMode(String s, RichTextBox r, RichTextBox o) { var t2 = new TextRange(o.Document.ContentStart, o.Document.ContentEnd); t2.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal); t2.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Black); var t = new TextRange(r.Document.ContentStart, r.Document.ContentEnd); var c = t.Start.GetInsertionPosition(LogicalDirection.Forward); t.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal); t.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Black); while (c != null) { string tr = c.GetTextInRun(LogicalDirection.Forward); if (!string.IsNullOrWhiteSpace(tr)) { int i = tr.IndexOf(s); if (i != -1) { var ss = c.GetPositionAtOffset(i, LogicalDirection.Forward); var se = ss.GetPositionAtOffset(s.Length, LogicalDirection.Forward); var sel = new TextRange(ss, se); sel.Text = s; sel.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold); sel.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Red); r.Selection.Select(sel.Start, sel.End); r.Focus(); } } c = c.GetNextContextPosition(LogicalDirection.Forward); } }

    } // End Class
} // End Namespace