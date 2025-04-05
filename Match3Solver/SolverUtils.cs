// FILE: Match3Solver/SolverUtils.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Shapes;
using System.Windows.Media;
using System.IO;

namespace Match3Solver
{
    // Struct to hold info for the debug mask (Keep this)
    struct DebugPixelInfo
    {
        public int X;
        public int Y;
        public int BoardValue; // The determined value (0-7, or 9 for unknown)
        public System.Windows.Media.Color SampledColor; // Store the actual sampled color
        public System.Windows.Media.Color MatchedColor; // Store the color it matched to
    }

    public class SolverUtils : SolverInterface
    {
        // --- !!! CALIBRATION CONSTANTS - MUST BE SET FOR HP1 !!! ---
        // Replace with values found using the calibration steps above
        private const int BOARD_START_X = 39;     // Example: Pixel X of top-left gem's top-left corner
        private const int BOARD_START_Y = 90;    // Example: Pixel Y of top-left gem's top-left corner
        private const int GEM_OFFSET_X = 70;      // Example: Pixel distance between gem starts (horizontal)
        private const int GEM_OFFSET_Y = 70;      // Example: Pixel distance between gem starts (vertical)
        // --- Sampling point within the gem (relative to top-left of gem) ---
        private const int SAMPLE_OFFSET_X = GEM_OFFSET_X / 2; // Sample near center X
        private const int SAMPLE_OFFSET_Y = GEM_OFFSET_Y / 2; // Sample near center Y
        // --- !!! END CALIBRATION CONSTANTS !!! ---

        // --- HP1 rawColor Array (8 types + Unused + Unknown) ---
        // Verify these RGB values using a color picker on an HP1 screenshot!
        System.Windows.Media.Color[] rawColor = new System.Windows.Media.Color[] {
            System.Windows.Media.Color.FromRgb(0xE9, 0xD0, 0x6C), // 0 - Joy        (Yellow/Gold Bell)
            System.Windows.Media.Color.FromRgb(0x49, 0xBA, 0xC0), // 1 - Sentiment  (Cyan Teardrop)
            System.Windows.Media.Color.FromRgb(0xF5, 0x58, 0xB8), // 2 - Passion    (Pink Heart)
            System.Windows.Media.Color.FromRgb(0xC4, 0x76, 0x18), // 3 - Romance    (Orange Lips?) - Often looks brownish/dark orange
            System.Windows.Media.Color.FromRgb(0x11, 0x5F, 0xA9), // 4 - Talent     (Blue Star?)
            System.Windows.Media.Color.FromRgb(0xE3, 0x4E, 0x3E), // 5 - Sexuality  (Red Whip?)
            System.Windows.Media.Color.FromRgb(0x84, 0xB2, 0x43), // 6 - Flirtation (Green Diamond?)
            System.Windows.Media.Color.FromRgb(0x56, 0x10, 0x77), // 7 - Broken Heart (Purple) 
            // --- Placeholders for indices 8 and 9 ---
            System.Windows.Media.Color.FromArgb(0, 0, 0, 0),      // 8 - UNUSED
            System.Windows.Media.Color.FromArgb(0, 0, 0, 0)       // 9 - Unknown / Empty (Will be represented by index 9 later)
        };
        // --- Make mapping explicit ---
        private readonly Dictionary<int, string> tileNames = new Dictionary<int, string> {
            {0, "Joy"}, {1, "Sentiment"}, {2, "Passion"}, {3, "Romance"},
            {4, "Talent"}, {5, "Sexuality"}, {6, "Flirtation"}, {7, "Broken"} , {9, "Unknown"}
        };


        int length; // Should be 7 (rows)
        int width;  // Should be 8 (cols)
        System.Windows.Shapes.Rectangle[][] boardDisplay; // Reference passed from MainWindow
        private UIFunctions uiFunctionsHelper; // Store reference to access colors

        public SolverUtils(int length, int width, System.Windows.Shapes.Rectangle[][] boardDisplay)
        {
            this.length = length; this.width = width; this.boardDisplay = boardDisplay;
            this.uiFunctionsHelper = new UIFunctions(null);
        }

        // --- Updated parseImage with Debug Mask Logic ---
        public int[][] parseImage(Bitmap bmp, bool createDebugMask = false)
        {
            // Initialize board with '9' (Unknown)
            int[][] board = new int[this.length][];
            for (int y = 0; y < this.length; y++)
            {
                board[y] = new int[this.width];
                for (int x = 0; x < this.width; x++)
                {
                    board[y][x] = 9; // Use 9 for unknown/initial state
                }
            }

            if (bmp == null)
            {
                Console.WriteLine("[ERROR] parseImage received a null bitmap.");
                return board; // Return the initialized (unknown) board
            }

            int sizeWidth = bmp.Width;
            int sizeLength = bmp.Height;
            BitmapData bitmapData = null;
            List<DebugPixelInfo> debugPixels = createDebugMask ? new List<DebugPixelInfo>() : null; // Only allocate if needed

            Console.WriteLine($"--- Parsing Image ({sizeWidth}x{sizeLength}) ---");
            Console.WriteLine($"Board Size: {this.length}x{this.width}");
            Console.WriteLine($"Calibration: Start({BOARD_START_X},{BOARD_START_Y}), Offset({GEM_OFFSET_X},{GEM_OFFSET_Y}), Sample({SAMPLE_OFFSET_X},{SAMPLE_OFFSET_Y})");

            // Use 'using' for the Bitmap clone if creating a mask
            Bitmap bmpForMask = null;
            Graphics g = null;
            if (createDebugMask)
            {
                try
                {
                    // --- Critical: Work on a clone for drawing the mask ---
                    bmpForMask = (Bitmap)bmp.Clone();
                    g = Graphics.FromImage(bmpForMask);
                    Console.WriteLine("Created clone and graphics context for debug mask.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to clone bitmap or create graphics for mask: {ex.Message}");
                    createDebugMask = false; // Disable mask creation if clone fails
                    if (g != null) g.Dispose(); // Clean up graphics if created
                    if (bmpForMask != null) bmpForMask.Dispose(); // Clean up clone if created
                    g = null;
                    bmpForMask = null;
                }
            }

            try
            {
                // --- FIX: Fully Qualify PixelFormat ---
                System.Drawing.Imaging.PixelFormat pixelFormat = bmp.PixelFormat;

                // --- FIX: Fully Qualify PixelFormat ---
                if ((pixelFormat & System.Drawing.Imaging.PixelFormat.Indexed) == System.Drawing.Imaging.PixelFormat.Indexed)
                {
                    Console.WriteLine($"[WARNING] Original PixelFormat ({pixelFormat}) is indexed. Attempting conversion to 32bppArgb.");
                    // --- FIX: Fully Qualify PixelFormat ---
                    using (Bitmap convertedBmp = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (Graphics gr = Graphics.FromImage(convertedBmp)) { gr.DrawImage(bmp, new System.Drawing.Rectangle(0, 0, convertedBmp.Width, convertedBmp.Height)); } // Use System.Drawing.Rectangle
                        pixelFormat = convertedBmp.PixelFormat; // Update format
                        // --- FIX: Fully Qualify Rectangle ---
                        bitmapData = convertedBmp.LockBits(new System.Drawing.Rectangle(0, 0, sizeWidth, sizeLength), ImageLockMode.ReadOnly, pixelFormat);
                        Console.WriteLine($"Converted to PixelFormat: {pixelFormat}");
                    }
                }
                else
                {
                    // --- FIX: Fully Qualify Rectangle ---
                    bitmapData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, sizeWidth, sizeLength), ImageLockMode.ReadOnly, pixelFormat);
                    Console.WriteLine($"Using original PixelFormat: {pixelFormat}");
                }


                int bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;
                if (bytesPerPixel < 3)
                {
                    Console.WriteLine($"[ERROR] Unsupported PixelFormat for reading ({pixelFormat}). Cannot get RGB.");
                    return board;
                }
                Console.WriteLine($"Bytes Per Pixel: {bytesPerPixel}");

                for (int y = 0; y < this.length; y++) // Rows 0-6
                {
                    for (int x = 0; x < this.width; x++) // Cols 0-7
                    {
                        // Calculate the *center* of the current gem's area
                        int gemTopLeftX = BOARD_START_X + (x * GEM_OFFSET_X);
                        int gemTopLeftY = BOARD_START_Y + (y * GEM_OFFSET_Y);
                        // Calculate the sampling point relative to the image top-left
                        int currentXPixel = gemTopLeftX + SAMPLE_OFFSET_X;
                        int currentYPixel = gemTopLeftY + SAMPLE_OFFSET_Y;

                        int currentBoardValue = 9; // Default to unknown
                        System.Windows.Media.Color sampleColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0); // Default transparent
                        System.Windows.Media.Color closestColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0); // Default transparent


                        if (currentXPixel >= 0 && currentXPixel < sizeWidth && currentYPixel >= 0 && currentYPixel < sizeLength)
                        {
                            byte[] rgb = getPixel(currentXPixel, currentYPixel, bitmapData, bytesPerPixel);
                            sampleColor = System.Windows.Media.Color.FromRgb(rgb[0], rgb[1], rgb[2]);
                            closestColor = GetClosestColor(rawColor, sampleColor); // Find the best match
                            int tileIndex = Array.IndexOf(rawColor, closestColor);

                            // Check if a valid color was found (index 0-7)
                            if (tileIndex >= 0 && tileIndex <= 7)
                            {
                                currentBoardValue = tileIndex;
                            }
                            else
                            {
                                currentBoardValue = 9; // Explicitly unknown if no good match
                                closestColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0); // Indicate no match for debug
                            }

                            // Reduced logging to avoid spam
                            if (y < 2 && x < 2) // Log first few tiles only
                            {
                                string name = tileNames.ContainsKey(currentBoardValue) ? tileNames[currentBoardValue] : "INVALID";
                                Console.WriteLine($"  Tile[{y},{x}]@({currentXPixel},{currentYPixel}): Smp=#{sampleColor.R:X2}{sampleColor.G:X2}{sampleColor.B:X2} -> Cls=#{closestColor.R:X2}{closestColor.G:X2}{closestColor.B:X2} (Idx={tileIndex}) -> Val={currentBoardValue} ({name})");
                            }
                        }
                        else
                        {
                            if (y < 2 && x < 2) Console.WriteLine($"  Tile[{y},{x}]@({currentXPixel},{currentYPixel}): Out of Bounds");
                            currentBoardValue = 9; // Out of bounds is unknown
                            sampleColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
                            closestColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
                        }

                        board[y][x] = currentBoardValue;

                        // Store info for debug mask drawing if enabled
                        if (createDebugMask && debugPixels != null)
                        {
                            debugPixels.Add(new DebugPixelInfo
                            {
                                X = currentXPixel,
                                Y = currentYPixel,
                                BoardValue = currentBoardValue,
                                SampledColor = sampleColor,
                                MatchedColor = closestColor // Store the matched color
                            });
                        }
                    }
                }
                Console.WriteLine($"--- Finished Sampling Pixels ---");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] during pixel sampling loop: {e.ToString()}");
            }
            finally
            {
                // --- Unlock the original bitmap ---
                // Important: Only unlock if bitmapData was successfully obtained
                if (bitmapData != null && bmp != null)
                {
                    try { bmp.UnlockBits(bitmapData); }
                    catch (Exception ex) { Console.WriteLine($"[ERROR] Unlocking bitmap failed: {ex.Message}"); }
                }
                // --- Do NOT dispose the original bmp here, it's owned by the caller ---
            }

            // --- Draw Debug Mask AFTER unlocking the source bitmap ---
            if (createDebugMask && bmpForMask != null && g != null && debugPixels != null)
            {
                Console.WriteLine("--- Drawing Debug Mask ---");
                try
                {
                    int markerSize = 5;
                    int textOffsetY = markerSize + 2;
                    using (System.Drawing.Font debugFont = new System.Drawing.Font("Arial", 8))
                    {
                        foreach (var info in debugPixels)
                        {
                            // Use the *matched* color from rawColor array for the marker,
                            // or Magenta if unknown (value 9 or no match)
                            System.Windows.Media.Color mediaColor = this.uiFunctionsHelper.myColors.ContainsKey(info.BoardValue)
                                ? this.uiFunctionsHelper.myColors[info.BoardValue]
                                : System.Windows.Media.Colors.Magenta; // Magenta for errors/unknown

                            System.Drawing.Color drawingColor = this.uiFunctionsHelper.MediaColorToDrawingColor(mediaColor);
                            using (SolidBrush brush = new SolidBrush(drawingColor))
                            {
                                int markerX = info.X - markerSize / 2;
                                int markerY = info.Y - markerSize / 2;
                                g.FillRectangle(brush, new System.Drawing.Rectangle(markerX, markerY, markerSize, markerSize));

                                // Optionally, draw text label (can get crowded)
                                // string label = tileNames.ContainsKey(info.BoardValue) ? tileNames[info.BoardValue].Substring(0,1) : "?";
                                // g.DrawString(label, debugFont, Brushes.White, info.X + markerSize, info.Y - textOffsetY);
                            }

                            // Draw a smaller dot of the *actual sampled* color next to it
                            System.Drawing.Color sampledDrawColor = this.uiFunctionsHelper.MediaColorToDrawingColor(info.SampledColor);
                            using (SolidBrush sampleBrush = new SolidBrush(sampledDrawColor))
                            {
                                g.FillEllipse(sampleBrush, info.X + markerSize, info.Y - markerSize / 2, 3, 3);
                            }
                        }
                    } // Font disposed

                    // Save the mask
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    // Use System.IO.Path explicitly
                    string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, $"debug_mask_hp1_{timestamp}.png");
                    bmpForMask.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"Debug mask saved to: {filePath}");
                }
                catch (Exception ex) { Console.WriteLine($"[ERROR] Drawing/saving debug mask failed: {ex}"); }
                finally
                {
                    // --- Dispose mask bitmap and graphics context ---
                    if (g != null) g.Dispose();
                    if (bmpForMask != null) bmpForMask.Dispose();
                }
            }

            return board;
        }
        // --- End parseImage ---


        public byte[] getPixel(int x, int y, BitmapData img, int bytesPerPixel)
        {
            // Check bounds *before* calculating pointer to prevent access violation
            if (x < 0 || x >= img.Width || y < 0 || y >= img.Height)
            {
                // Return a default value or throw, depending on desired handling
                //Console.WriteLine($"[WARN] getPixel OOB: ({x},{y}) vs ({img.Width},{img.Height})");
                return new byte[] { 0, 0, 0 }; // Return black for OOB
            }

            IntPtr pixelPtr = img.Scan0 + (y * img.Stride) + (x * bytesPerPixel);
            byte[] rgb = new byte[3];

            unsafe
            {
                byte* p = (byte*)pixelPtr.ToPointer();
                // Common formats: BGR (24bpp) or BGRA (32bpp)
                rgb[0] = p[2]; // R
                rgb[1] = p[1]; // G
                rgb[2] = p[0]; // B
            }
            return rgb;
        }


        private static System.Windows.Media.Color GetClosestColor(System.Windows.Media.Color[] colorArray, System.Windows.Media.Color baseColor)
        {
            double minDiff = double.MaxValue;
            System.Windows.Media.Color bestMatch = System.Windows.Media.Color.FromArgb(0, 0, 0, 0); // Default transparent

            // Iterate through the predefined colors (indices 0-7 for HP1)
            for (int i = 0; i < 8; i++) // Only check the 8 valid gem colors
            {
                if (i >= colorArray.Length) break; // Safety check
                System.Windows.Media.Color candidateColor = colorArray[i];

                // Calculate RGB distance (ignoring alpha)
                double diff = GetDiffColorDouble(candidateColor, baseColor);

                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestMatch = candidateColor;
                }
            }

            // Optional: Add a threshold. If minDiff is too large, maybe it's not a match.
            const double MAX_ACCEPTABLE_DIFF_SQ = 5000; // Adjust this threshold based on testing!
            if (minDiff > MAX_ACCEPTABLE_DIFF_SQ)
            {
                //Console.WriteLine($"Sample {baseColor.R:X2}{baseColor.G:X2}{baseColor.B:X2} failed threshold ({minDiff} > {MAX_ACCEPTABLE_DIFF_SQ}). Closest was {bestMatch.R:X2}{bestMatch.G:X2}{bestMatch.B:X2}.");
                return System.Windows.Media.Color.FromArgb(0, 0, 0, 0); // Return transparent if no good match
            }


            return bestMatch;
        }

        // Use double for potentially better precision during comparison, though int is usually fine
        private static double GetDiffColorDouble(System.Windows.Media.Color color, System.Windows.Media.Color baseColor)
        {
            double dr = color.R - baseColor.R;
            double dg = color.G - baseColor.G;
            double db = color.B - baseColor.B;
            return dr * dr + dg * dg + db * db; // Euclidean distance squared (faster than sqrt)
        }


        // --- Restore loopBoard (HP2 Sliding Logic) ---
        public List<SolverInterface.Movement> loopBoard(int[][] board2Test)
        {
            List<SolverInterface.Movement> returnThis = new List<SolverInterface.Movement>();
            if (board2Test == null) return returnThis; // Safety check

            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Check Horizontal Moves
                    for (int offset = 1; x + offset < width; offset++) // Move Right
                    {
                        SimulateAndEvaluateMove(board2Test, x, y, false, offset, ref returnThis);
                    }
                    for (int offset = -1; x + offset >= 0; offset--) // Move Left
                    {
                        SimulateAndEvaluateMove(board2Test, x, y, false, offset, ref returnThis);
                    }

                    // Check Vertical Moves
                    for (int offset = 1; y + offset < length; offset++) // Move Down
                    {
                        SimulateAndEvaluateMove(board2Test, x, y, true, offset, ref returnThis);
                    }
                    for (int offset = -1; y + offset >= 0; offset--) // Move Up
                    {
                        SimulateAndEvaluateMove(board2Test, x, y, true, offset, ref returnThis);
                    }
                }
            }

            return returnThis;
        }

        // Helper for loopBoard
        private void SimulateAndEvaluateMove(int[][] originalBoard, int x, int y, bool isVertical, int offset, ref List<SolverInterface.Movement> resultsList)
        {
            // DEEP COPY
            int[][] boardCopy = Array.ConvertAll(originalBoard, a => (int[])a.Clone());

            // MOVE
            if (isVertical)
                boardCopy = moveVertical(y, x, offset, boardCopy);
            else
                boardCopy = moveHorizontal(y, x, offset, boardCopy);

            int boardHash = getBoardHash(boardCopy);

            // ONLY SAVE IF THERE WAS A MATCH/SCORE
            SolverInterface.Score resultScore = evalBoard(new SolverInterface.Score(), boardCopy, true); // Start fresh score evaluation

            if (resultScore.hasScore()) // Check if the move resulted in any score
            {
                // Check if board pattern already exists to reduce duplicates
                // Note: Simple hash collision is possible but unlikely to be a major issue here.
                if (!resultsList.Any(s => s.boardHash == boardHash))
                {
                    resultsList.Add(new SolverInterface.Movement(x, y, isVertical, offset, resultScore, boardHash));
                }
                // else { Console.WriteLine($"Duplicate board state detected for move [{y},{x}] {(isVertical?"V":"H")}{offset}, hash {boardHash}"); }
            }
        }


        // --- Restore moveHorizontal (Adapted for HP1 dimensions if needed, but logic is general) ---
        public int[][] moveHorizontal(int yPos, int xPos, int amount, int[][] board2Test)
        {
            int[][] board2TestCopy = Array.ConvertAll(board2Test, a => (int[])a.Clone());
            int newX = xPos + amount;

            if (amount == 0 || newX >= width || newX < 0) // Check against HP1 width
            {
                // Console.WriteLine($"Out of Bounds moveHorizontal: [{yPos},{xPos}] by {amount}");
                return board2TestCopy; // Return original on invalid move
            }

            int[] line = board2TestCopy[yPos];
            int value = line[xPos]; // Value being moved

            if (amount > 0) // Moving Right
            {
                for (int count = 0; count < amount; count++)
                {
                    line[xPos + count] = line[xPos + count + 1]; // Shift cells left
                }
            }
            else // Moving Left (amount is negative)
            {
                for (int count = 0; count < -amount; count++)
                {
                    line[xPos - count] = line[xPos - count - 1]; // Shift cells right
                }
            }
            line[newX] = value; // Place moved value in the destination
            // No need to reassign board2TestCopy[yPos] = line; modification is in place.
            return board2TestCopy;
        }

        // --- Restore moveVertical (Adapted for HP1 dimensions if needed) ---
        public int[][] moveVertical(int yPos, int xPos, int amount, int[][] board2Test)
        {
            int[][] board2TestCopy = Array.ConvertAll(board2Test, a => (int[])a.Clone());
            int newY = yPos + amount;

            if (amount == 0 || newY >= length || newY < 0) // Check against HP1 length
            {
                // Console.WriteLine($"Out of Bounds moveVertical: [{yPos},{xPos}] by {amount}");
                return board2TestCopy;
            }

            int value = board2TestCopy[yPos][xPos];

            if (amount > 0) // Moving Down
            {
                for (int count = 0; count < amount; count++)
                {
                    board2TestCopy[yPos + count][xPos] = board2TestCopy[yPos + count + 1][xPos]; // Shift cells up
                }
            }
            else // Moving Up
            {
                for (int count = 0; count < -amount; count++)
                {
                    board2TestCopy[yPos - count][xPos] = board2TestCopy[yPos - count - 1][xPos]; // Shift cells down
                }
            }
            board2TestCopy[newY][xPos] = value;
            return board2TestCopy;
        }

        // --- Implement evalBoard (Chain Reaction Simulation) ---
        public SolverInterface.Score evalBoard(SolverInterface.Score currentScore, int[][] boardState, bool isInitialMove)
        {
            // Create a copy to mark matches without modifying the input state directly yet
            int[][] markedBoard = Array.ConvertAll(boardState, a => (int[])a.Clone());
            bool matchesFound = checkMatch(markedBoard); // checkMatch now modifies markedBoard and returns true if matches found

            if (matchesFound)
            {
                // A match occurred in this step
                if (!isInitialMove)
                {
                    currentScore.chains++; // Increment chain counter only after the first move
                }

                // Extract score from the marked gems and update the board state (set matched to 9)
                extractScore(ref currentScore, markedBoard, isInitialMove); // Updates currentScore and sets matched gems in markedBoard to 9

                // Apply gravity to the modified board state (where matched gems are now 9)
                int[][] boardAfterGravity = gravityFall(markedBoard);

                // Recursively call evalBoard with the new board state and updated score
                return evalBoard(currentScore, boardAfterGravity, false); // Next call is not the initial move
            }
            else
            {
                // No matches found in this step, the chain reaction ends. Return the accumulated score.
                return currentScore;
            }
        }


        // --- Implement checkMatch (Marks matches on the board) ---
        // Returns true if any matches were found and marked, false otherwise.
        public bool checkMatch(int[][] boardToCheck) // Modifies boardToCheck directly
        {
            bool matchFound = false;
            // Use a temporary boolean board to mark gems to be removed without modifying
            // values immediately, preventing overlapping matches from interfering.
            bool[][] markedForRemoval = new bool[length][];
            for (int y = 0; y < length; y++) markedForRemoval[y] = new bool[width];

            // Check Horizontal Matches
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x <= width - 3; x++) // Stop 2 columns early
                {
                    int currentTile = boardToCheck[y][x] % 10; // Use modulo for base type
                    if (currentTile == 9) continue; // Skip empty/unknown

                    if (boardToCheck[y][x + 1] % 10 == currentTile && boardToCheck[y][x + 2] % 10 == currentTile)
                    {
                        // Match of 3 found
                        markedForRemoval[y][x] = true;
                        markedForRemoval[y][x + 1] = true;
                        markedForRemoval[y][x + 2] = true;
                        matchFound = true;

                        // Check for match of 4
                        if (x + 3 < width && boardToCheck[y][x + 3] % 10 == currentTile)
                        {
                            markedForRemoval[y][x + 3] = true;
                            // Check for match of 5
                            if (x + 4 < width && boardToCheck[y][x + 4] % 10 == currentTile)
                            {
                                markedForRemoval[y][x + 4] = true;
                            }
                        }
                        x += 2; // Skip checked tiles in this row
                    }
                }
            }

            // Check Vertical Matches
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y <= length - 3; y++) // Stop 2 rows early
                {
                    int currentTile = boardToCheck[y][x] % 10;
                    if (currentTile == 9) continue;

                    if (boardToCheck[y + 1][x] % 10 == currentTile && boardToCheck[y + 2][x] % 10 == currentTile)
                    {
                        markedForRemoval[y][x] = true;
                        markedForRemoval[y + 1][x] = true;
                        markedForRemoval[y + 2][x] = true;
                        matchFound = true;

                        if (y + 3 < length && boardToCheck[y + 3][x] % 10 == currentTile)
                        {
                            markedForRemoval[y + 3][x] = true;
                            if (y + 4 < length && boardToCheck[y + 4][x] % 10 == currentTile)
                            {
                                markedForRemoval[y + 4][x] = true;
                            }
                        }
                        y += 2; // Skip checked tiles in this column
                    }
                }
            }

            // --- Apply the markings to the board by adding 10 ---
            // This separates detection from modification slightly better
            if (matchFound)
            {
                for (int y = 0; y < length; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (markedForRemoval[y][x] && boardToCheck[y][x] < 10) // Check it hasn't already been marked by overlapping match
                        {
                            boardToCheck[y][x] += 10; // Mark for scoring/removal
                        }
                    }
                }
            }

            return matchFound;
        }


        // --- Implement gravityFall ---
        private int[][] gravityFall(int[][] boardState)
        {
            // Works from bottom up, column by column
            for (int x = 0; x < width; x++)
            {
                int emptyRow = length - 1; // Start checking from the bottom row for empty spots

                // Find the lowest empty row in the current column
                while (emptyRow >= 0 && boardState[emptyRow][x] != 9)
                {
                    emptyRow--;
                }

                // If no empty spots found in this column, continue to the next
                if (emptyRow < 0) continue;

                // Start looking for gems to drop from above the lowest empty spot
                for (int checkRow = emptyRow - 1; checkRow >= 0; checkRow--)
                {
                    if (boardState[checkRow][x] != 9) // Found a gem to drop
                    {
                        // Move the gem down to the emptyRow
                        boardState[emptyRow][x] = boardState[checkRow][x];
                        // Set the original position to empty
                        boardState[checkRow][x] = 9;
                        // Move the emptyRow marker up to the next potential empty spot
                        emptyRow--;
                    }
                }
            }
            return boardState;
        }

        // --- Implement extractScore (Modified to take score by ref) ---
        public void extractScore(ref SolverInterface.Score score, int[][] boardState, bool isInitialMove) // Modifies boardState
        {
            int currentMoveCost = 0;
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int value = boardState[y][x];
                    if (value > 9) // Check if marked for scoring (value > 9)
                    {
                        score.addScoreFromValue(value); // Add score for this tile type
                        boardState[y][x] = 9; // Set the board position to empty/unknown
                        currentMoveCost++; // Increment cost count for this evaluation step
                    }
                }
            }
            // Only add to staminaCost on the *initial* move evaluation that causes matches.
            // Subsequent chain reactions don't cost extra moves in HP1.
            if (isInitialMove && currentMoveCost > 0)
            {
                // HP1: 1 move = 1 cost, regardless of how many gems match initially.
                // Joy gems might alter this later if needed.
                score.staminaCost = 1;
            }
            // score.wasChanged is handled by addScoreFromValue
        }

        // --- Implement sortList (Adapt based on HP1 Score structure) ---
        public List<SolverInterface.Movement> sortList(List<SolverInterface.Movement> results, int sortingMode)
        {
            if (results == null) return new List<SolverInterface.Movement>();

            switch (sortingMode)
            {
                case 1: // Chain First
                    return results.OrderByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal()) // Net score as secondary
                                  .ThenByDescending(m => m.score.Joy)       // Joy as tertiary (HP1 specific)
                                  .ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 2: // Net Score First
                    return results.OrderByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.Joy)
                                  .ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 3: // 4/5 Match First (using staminaCost as proxy for # matched)
                    // Use customCompare for 'amount matched' (staminaCost proxy)
                    return results.OrderByDescending(m => m.score.staminaCost, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy)
                                  .ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 4: // Passion First
                    return results.OrderByDescending(m => m.score.Passion, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy)
                                  .ToList();
                case 5: // Joy First
                    return results.OrderByDescending(m => m.score.Joy, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 6: // Sentiment First
                    return results.OrderByDescending(m => m.score.Sentiment, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy).ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 7: // Talent First
                    return results.OrderByDescending(m => m.score.Talent, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy).ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 8: // Flirtation First
                    return results.OrderByDescending(m => m.score.Flirtation, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy).ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 9: // Romance First
                    return results.OrderByDescending(m => m.score.Romance, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy).ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 10: // Sexuality First
                    return results.OrderByDescending(m => m.score.Sexuality, new customCompare())
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal())
                                  .ThenByDescending(m => m.score.Joy).ThenByDescending(m => m.score.Passion)
                                  .ToList();
                case 11: // Unused (was Stamina) - Maybe sort by Raw Gain?
                    Console.WriteLine("Sorting Mode 11 (Raw Gain) selected.");
                    return results.OrderByDescending(m => m.score.getTotalNoBroken()) // Raw score
                                 .ThenByDescending(m => m.score.chains)
                                 .ThenByDescending(m => m.score.getTotal()) // Net as secondary
                                 .ThenByDescending(m => m.score.Joy)
                                 .ThenByDescending(m => m.score.Passion)
                                 .ToList();
                case 12: // Broken Heart First (Lowest is best)
                         // Use customCompareBroken to prioritize non-zero broken hearts, but sort ascendingly
                    return results.OrderBy(m => m.score.BrokenHeart, new customCompareBrokenAsc()) // Sort ascending for broken hearts
                                  .ThenByDescending(m => m.score.chains)
                                  .ThenByDescending(m => m.score.getTotal()) // Higher net score better when broken hearts are equal
                                  .ThenByDescending(m => m.score.Joy)
                                  .ThenByDescending(m => m.score.Passion)
                                  .ToList();
                default:
                    Console.WriteLine($"Warning: Unknown sortingMode {sortingMode}. Defaulting to Chain First.");
                    goto case 1; // Default to Chain First
            }
        }

        // --- Restore getBoardHash ---
        private int getBoardHash(int[][] board2TestCopy)
        {
            // Flatten the 2D array to 1D
            int[] flattenedBoard = board2TestCopy.SelectMany(row => row).ToArray();
            // Convert to a string representation
            // Using string.Join for potentially better performance with large arrays vs StringBuilder loop
            string boardString = string.Join(",", flattenedBoard); // Use comma separator
                                                                   // Return the hash code of the string
            return boardString.GetHashCode();
        }


        // --- Helper Comparer Classes (Keep customCompare, modify customCompareBroken) ---

        /// <summary>
        /// PRIORITIZES SCORE: 5 > 4 > normal sort descending (higher is better). 0 is worst.
        /// </summary>
        class customCompare : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x == y) return 0;
                if (x == 0) return -1; // 0 is always worse than non-zero
                if (y == 0) return 1;  // Non-zero is always better than 0

                bool xMod5 = (x % 5 == 0 && x > 0); // Ensure positive for mod check
                bool yMod5 = (y % 5 == 0 && y > 0);
                bool xMod4 = (x % 4 == 0 && x > 0);
                bool yMod4 = (y % 4 == 0 && y > 0);

                if (xMod5 && yMod5) return Comparer<int>.Default.Compare(x, y); // Both div by 5, compare values
                if (xMod5) return 1; // x is div by 5, y is not
                if (yMod5) return -1; // y is div by 5, x is not

                if (xMod4 && yMod4) return Comparer<int>.Default.Compare(x, y); // Both div by 4, compare values
                if (xMod4) return 1; // x is div by 4, y is not
                if (yMod4) return -1; // y is div by 4, x is not

                return Comparer<int>.Default.Compare(x, y); // Neither are special matches, compare values
            }
        }

        /// <summary>
        /// Special comparer for Broken Hearts: Lower is better. 0 is best.
        /// Sorts ASCENDINGLY.
        /// </summary>
        class customCompareBrokenAsc : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                // Standard ascending comparison, lower values come first.
                return Comparer<int>.Default.Compare(x, y);
            }
        }


    } // End Class
} // End Namespace