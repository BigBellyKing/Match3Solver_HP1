using System;
using System.Collections.Generic;
using System.Drawing; // For Bitmap, Graphics, Point, Size, SolidBrush
using System.Drawing.Imaging; // For BitmapData, PixelFormat, ImageLockMode, ImageFormat
using System.Linq;
using System.Windows.Shapes; // For boardDisplay type (passed in constructor)
using System.Windows.Media; // For Color struct used in rawColor, GetClosestColor
using System.IO; // Needed for Path

namespace Match3Solver
{
    // Struct to hold info for the debug mask
    struct DebugPixelInfo
    {
        public int X;
        public int Y;
        public int BoardValue; // The determined value (0-7, or 9 for unknown)
    }

    public class SolverUtils : SolverInterface
    {
        // --- HP1 rawColor Array (8 types + Unused + Unknown) ---
        System.Windows.Media.Color[] rawColor = new System.Windows.Media.Color[] {
            System.Windows.Media.Color.FromRgb(0xE9, 0xD0, 0x6C), // 0 - Joy
            System.Windows.Media.Color.FromRgb(0x49, 0xBA, 0xC0), // 1 - Sentiment
            System.Windows.Media.Color.FromRgb(0xF5, 0x58, 0xB8), // 2 - Passion
            System.Windows.Media.Color.FromRgb(0xC4, 0x76, 0x18), // 3 - Romance
            System.Windows.Media.Color.FromRgb(0x11, 0x5F, 0xA9), // 4 - Talent
            System.Windows.Media.Color.FromRgb(0xE3, 0x4E, 0x3E), // 5 - Sexuality
            System.Windows.Media.Color.FromRgb(0x84, 0xB2, 0x43), // 6 - Flirtation
            System.Windows.Media.Color.FromRgb(0xB9, 0x68, 0xD5), // 7 - Broken Heart
            System.Windows.Media.Color.FromArgb(0, 0, 0, 0),      // 8 - UNUSED placeholder
            System.Windows.Media.Color.FromArgb(0, 0, 0, 0)       // 9 - Unknown / Empty
        };

        int length; // 7
        int width;  // 8
        System.Windows.Shapes.Rectangle[][] boardDisplay; // Reference passed from MainWindow
        private UIFunctions uiFunctionsHelper; // Store reference to access colors

        public SolverUtils(int length, int width, System.Windows.Shapes.Rectangle[][] boardDisplay)
        {
            this.length = length; this.width = width; this.boardDisplay = boardDisplay;
            // Create helper instance - passing null for parent is okay if we only need colors/helpers
            this.uiFunctionsHelper = new UIFunctions(null);
        }

        // --- parseImage with Debug Mask Logic ---
        public int[][] parseImage(Bitmap bmp, bool createDebugMask = false)
        {
            int[][] board = new int[this.length][];
            for (int i = 0; i < this.length; i++) { board[i] = new int[this.width]; for (int j = 0; j < this.width; j++) board[i][j] = 9; }
            if (bmp == null) { Console.WriteLine("Error: parseImage null bitmap."); return board; }

            int sizeWidth = bmp.Width; int sizeLength = bmp.Height;
            BitmapData bitmapData = null;
            List<DebugPixelInfo> debugPixels = new List<DebugPixelInfo>();

            try
            {
                // --- !!! VITAL CALIBRATION AREA - REPLACE THESE !!! ---
                int startX = 50;        // PIXEL X of top-left gem's top-left corner
                int startY = 124;       // PIXEL Y of top-left gem's top-left corner
                int offsetX = 87;       // PIXEL distance between gem starts (horizontal)
                int offsetY = 87;       // PIXEL distance between gem starts (vertical)
                int sampleOffsetX = offsetX / 2; // Offset to sample near center
                int sampleOffsetY = offsetY / 2;
                // --- !!! END CALIBRATION AREA !!! ---

                Console.WriteLine($"--- Parsing Image ({sizeWidth}x{sizeLength}) ---");
                Console.WriteLine($"Using StartX:{startX}, StartY:{startY}, OffsetX:{offsetX}, OffsetY:{offsetY}, SampleOffset:({sampleOffsetX},{sampleOffsetY})");

                System.Drawing.Imaging.PixelFormat pixelFormat = bmp.PixelFormat;
                int bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;
                if (bytesPerPixel < 3) { Console.WriteLine($"Error: Unsupported PixelFormat({pixelFormat})"); return board; }
                Console.WriteLine($"Bitmap PixelFormat:{pixelFormat}, BPP:{bytesPerPixel}");

                bitmapData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, sizeWidth, sizeLength), ImageLockMode.ReadOnly, pixelFormat);

                for (int y = 0; y < this.length; y++)
                { // Rows 0-6
                    for (int x = 0; x < this.width; x++)
                    { // Cols 0-7
                        int currentXPixel = startX + (x * offsetX) + sampleOffsetX;
                        int currentYPixel = startY + (y * offsetY) + sampleOffsetY;
                        int currentBoardValue = 9;

                        if (currentXPixel >= 0 && currentXPixel < sizeWidth && currentYPixel >= 0 && currentYPixel < sizeLength)
                        {
                            byte[] rgb = getPixel(currentXPixel, currentYPixel, bitmapData, bytesPerPixel);
                            System.Windows.Media.Color sampleColor = System.Windows.Media.Color.FromRgb(rgb[0], rgb[1], rgb[2]);
                            System.Windows.Media.Color closestColor = GetClosestColor(rawColor, sampleColor);
                            int tileIndex = Array.IndexOf(rawColor, closestColor);
                            currentBoardValue = (tileIndex >= 0 && tileIndex <= 7) ? tileIndex : 9;

                            if (y == 0 || (y == this.length - 1 && x == this.width - 1))
                            { // Less spammy debug
                                Console.WriteLine($"  Tile[{y},{x}]@({currentXPixel},{currentYPixel}): Smp=#{sampleColor.R:X2}{sampleColor.G:X2}{sampleColor.B:X2} -> Cls=#{closestColor.R:X2}{closestColor.G:X2}{closestColor.B:X2} -> Idx={tileIndex} -> Val={currentBoardValue}");
                            }
                        }
                        else
                        {
                            if (y == 0 || (y == this.length - 1 && x == this.width - 1)) { Console.WriteLine($"  Tile[{y},{x}]@({currentXPixel},{currentYPixel}): OOB"); }
                        }
                        board[y][x] = currentBoardValue;
                        debugPixels.Add(new DebugPixelInfo { X = currentXPixel, Y = currentYPixel, BoardValue = currentBoardValue });
                    }
                }
                Console.WriteLine($"--- Finished Sampling Pixels ---");
            }
            catch (Exception e) { Console.WriteLine($"Error during pixel sampling: {e}"); }
            finally { if (bitmapData != null && bmp != null) try { bmp.UnlockBits(bitmapData); } catch (Exception ex) { Console.WriteLine($"Unlock error:{ex.Message}"); } }

            if (createDebugMask)
            {
                Console.WriteLine("--- Creating Debug Mask ---");
                try
                {
                    // Create the clone and use 'using' to ensure it's disposed
                    using (Bitmap bmpToWrite = new Bitmap(bmp)) // Work on a clone for safety
                    {
                        // Use 'using' for the Graphics object, tied to the clone
                        using (Graphics g = Graphics.FromImage(bmpToWrite))
                        {
                            int markerSize = 5;
                            foreach (var info in debugPixels)
                            {
                                // Access myColors via helper instance (now internal)
                                System.Windows.Media.Color mediaColor = this.uiFunctionsHelper.myColors.ContainsKey(info.BoardValue)
                                    ? this.uiFunctionsHelper.myColors[info.BoardValue]
                                    : System.Windows.Media.Colors.Magenta; // Magenta for errors

                                System.Drawing.Color drawingColor = this.uiFunctionsHelper.MediaColorToDrawingColor(mediaColor);
                                using (SolidBrush brush = new SolidBrush(drawingColor))
                                {
                                    int markerX = info.X - markerSize / 2; int markerY = info.Y - markerSize / 2;
                                    g.FillRectangle(brush, new System.Drawing.Rectangle(markerX, markerY, markerSize, markerSize));
                                }
                            }
                        } // Graphics object 'g' is disposed here

                        // ---- MOVED SAVE LOGIC HERE ----
                        // Now save the modified bitmap. It's still in scope until the outer 'using' block ends.
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        // Use System.IO.Path explicitly
                        string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, $"debug_mask_{timestamp}.png");
                        bmpToWrite.Save(filePath, System.Drawing.Imaging.ImageFormat.Png); // Save the modified clone
                        Console.WriteLine($"Debug mask saved to: {filePath}");
                        // -----------------------------

                    } // Bitmap object 'bmpToWrite' is disposed here
                }
                catch (Exception ex) { Console.WriteLine($"Error creating/saving debug mask: {ex}"); }
            }
            return board;
        }
        // --- End parseImage ---


        public byte[] getPixel(int x, int y, BitmapData img, int bytesPerPixel)
        {
            IntPtr pixelPtr = img.Scan0 + (y * img.Stride) + (x * bytesPerPixel);
            unsafe
            {
                byte* p = (byte*)pixelPtr.ToPointer();
                // Handle potential alpha channel (bytesPerPixel == 4) vs just RGB (bytesPerPixel == 3)
                // Assuming common formats like 32bppArgb (BGRA) or 24bppRgb (BGR)
                if (bytesPerPixel == 4)
                {
                    return new byte[] { p[2], p[1], p[0] }; // Return RGB from BGRA
                }
                else // Assuming bytesPerPixel == 3
                {
                    return new byte[] { p[2], p[1], p[0] }; // Return RGB from BGR
                }
                // Consider adding error handling or support for other formats if needed
            }
        }


        private static System.Windows.Media.Color GetClosestColor(System.Windows.Media.Color[] colorArray, System.Windows.Media.Color baseColor)
        {
            // Filter out fully transparent colors (like the unused/unknown placeholders) before calculating diff
            var colors = colorArray.Where(c => c.A > 0)
                                   .Select(x => new { Value = x, Diff = GetDiffColor(x, baseColor) })
                                   .ToList();

            if (!colors.Any())
            {
                // Return a default or handle error if no valid colors are in the array
                return System.Windows.Media.Color.FromArgb(0, 0, 0, 0); // Or throw?
            }

            var minDiff = colors.Min(x => x.Diff);

            // Find the first color that matches the minimum difference.
            // Using FirstOrDefault defensively in case the list somehow becomes empty between checks.
            var closestMatch = colors.FirstOrDefault(x => x.Diff == minDiff);

            // If a match is found, return its Value; otherwise, return a default.
            return closestMatch?.Value ?? System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
        }


        private static int GetDiffColor(System.Windows.Media.Color color, System.Windows.Media.Color baseColor)
        {
            // Including Alpha difference might be undesirable if comparing opaque colors to potentially semi-transparent ones.
            // Often, only RGB difference is needed for color matching.
            // int a = color.A - baseColor.A;
            int r = color.R - baseColor.R;
            int g = color.G - baseColor.G;
            int b = color.B - baseColor.B;
            // return a * a + r * r + g * g + b * b; // Original with Alpha
            return r * r + g * g + b * b; // Common RGB distance squared (Euclidean without sqrt)
        }

        // --- Placeholder Methods ---
        // Consider implementing these or removing them if they are truly unused.
        public List<SolverInterface.Movement> loopBoard(int[][] board2Test) { Console.WriteLine("Warning: loopBoard not implemented."); return new List<SolverInterface.Movement>(); }
        public SolverInterface.Score evalBoard(SolverInterface.Score score, int[][] board2Test, bool initial) { Console.WriteLine("Warning: evalBoard not implemented."); return score; }
        private int[][] gravityFall(int[][] board2Test) { Console.WriteLine("Warning: gravityFall not implemented."); return board2Test; }
        public int[][] checkMatch(int yPos, int xPos, int target, int[][] board2Test) { Console.WriteLine("Warning: checkMatch not implemented."); return board2Test; }
        public SolverInterface.Score extractScore(SolverInterface.Score score, int[][] board2Test, bool initial) { Console.WriteLine("Warning: extractScore not implemented."); return score; }
        public List<SolverInterface.Movement> sortList(List<SolverInterface.Movement> results, int sortingMode) { Console.WriteLine("Warning: sortList using placeholder logic."); return results ?? new List<SolverInterface.Movement>(); }

        // --- Helper Classes (Consider if needed) ---
        // These seem unused based on the placeholder `sortList` implementation.
        class customCompare : IComparer<int> { public int Compare(int x, int y) { return Comparer<int>.Default.Compare(x, y); } }
        class customCompareBroken : IComparer<int> { public int Compare(int x, int y) { return Comparer<int>.Default.Compare(x, y); } }

    }
}