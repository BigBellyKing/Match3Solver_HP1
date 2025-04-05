// FILE: Match3Solver/MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media; // Explicitly use this namespace
using System.Windows.Shapes;
using System.Windows.Documents;
using CrashReporterDotNET;
using System.Text;
using System.Linq;
using System.Diagnostics;
using Capture.Hook;
using Capture.Interface;
using System.IO; // Keep for Path ambiguity resolution
// Add System.Drawing using directive
using System.Drawing; // For Bitmap

namespace Match3Solver
{
    public partial class MainWindow : Window, SolverInterface
    {
        public int width = 8; // HP1 width
        public int length = 7; // HP1 height
        public int boxSize = 30;
        public int sortingMode = 1;

        public int[][] board = new int[7][];
        public System.Windows.Shapes.Rectangle[][] boardDisplay = new System.Windows.Shapes.Rectangle[7][]; // Use full namespace
        List<SolverInterface.Movement> results;
        public SolverUtils solver;
        public GameHook hook;
        public UIFunctions draw;
        // --- SET debugMode = true TO CREATE MASKS ---
        public Boolean debugMode = false; // CHANGE THIS TO false FOR NORMAL OPERATION
        // --- END DEBUG MODE ---
        private int lastScreenHeight = 0;
        private int lastScreenWidth = 0;
        private int selectedIndex = 0;
        private const string TargetProcessName = "HuniePop"; // Target HP1

        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000, MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008;
        // Define Keys (VK_MINUS is not needed for HP1 default controls)
        private const uint VK_I = 0x49, VK_C = 0x43, VK_0 = 0x30, VK_1 = 0x31, VK_2 = 0x32, VK_3 = 0x33, VK_4 = 0x34, VK_5 = 0x35, VK_6 = 0x36, VK_7 = 0x37, VK_8 = 0x38, VK_9 = 0x39, VK_PLUS = 0xBB, /*VK_MINUS = 0xBD,*/ VK_UP = 0x26, VK_DOWN = 0x28, VK_O = 0x4F;

        private IntPtr _windowHandle;
        private HwndSource _source;
        private static ReportCrash _reportCrash;
        private Thread waitLooper;
        private Boolean killIt = false;

        public MainWindow()
        {
            InitializeComponent();
            // Initialize board with 9 (unknown)
            for (int y = 0; y < length; y++)
            {
                board[y] = new int[width];
                for (int x = 0; x < width; x++) board[y][x] = 9;
            }
            statusText.Text = $"Waiting for {TargetProcessName}.";
            LaunchGameListener(); // Start listening immediately
            hook = new GameHook(statusText, this);
            solver = new SolverUtils(length, width, boardDisplay);
            draw = new UIFunctions(this);
            draw.initBoardDisplay(); // Create the visual grid
            results = new List<SolverInterface.Movement>(); // Initialize results list
            initCrashReporter(); // Setup crash reporting
        }

        // LaunchGameListener remains mostly the same, ensures it targets TargetProcessName
        public void LaunchGameListener()
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (killIt) break; // Allow thread to exit cleanly

                    bool gameRunning = false;
                    try
                    {
                        // Check if the target process is running
                        gameRunning = Process.GetProcessesByName(TargetProcessName).Length > 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking for process: {ex.Message}");
                        // Continue loop, maybe process access issue temporarily
                    }

                    if (gameRunning)
                    {
                        // If game is running and we are not hooked, attempt to attach
                        if (!hook.hooked)
                        {
                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                // Let AttachProcess handle status updates
                                hook.AttachProcess();
                            }));
                        }
                        // Once attached or found running, we can break the listener loop
                        // Or let it run to re-attach if hook detaches? Let's break for now.
                        break;
                    }
                    else
                    {
                        // Game not running, update status if not already hooked
                        if (!hook.hooked)
                        {
                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                statusText.Foreground = System.Windows.Media.Brushes.IndianRed; // Use full namespace
                                statusText.Text = $"Waiting for {TargetProcessName} to Open.";
                            }));
                        }
                    }

                    // Wait before checking again
                    Thread.Sleep(2000); // Check every 2 seconds
                }
                Console.WriteLine("GameListenerThread exiting.");

            })
            { IsBackground = true, Name = "GameListenerThread" }.Start();
        }


        // initCrashReporter remains the same
        private static void initCrashReporter() { AppDomain.CurrentDomain.UnhandledException += (s, a) => SendReport((Exception)a.ExceptionObject); byte[] d1 = Convert.FromBase64String("enFhYy56cWh1K3puZ3B1M2ZieWlyZXBlbmZ1ZXJjYmVnQHR6bnZ5LnBieg=="); string s1 = Encoding.UTF8.GetString(d1); byte[] d2 = Convert.FromBase64String("cjBwOW9vcHEtNzVzcC00cHJxLW8yNjktbnIzMTQ5MDc0OTQy"); string s2 = Encoding.UTF8.GetString(d2); _reportCrash = new ReportCrash(String.Join("", s1.Select(x => char.IsLetter(x) ? (x >= 65 && x <= 77) || (x >= 97 && x <= 109) ? (char)(x + 13) : (char)(x - 13) : x))) { Silent = true, ShowScreenshotTab = true, IncludeScreenshot = false, AnalyzeWithDoctorDump = true, DoctorDumpSettings = new DoctorDumpSettings { ApplicationID = new Guid(String.Join("", s2.Select(x => char.IsLetter(x) ? (x >= 65 && x <= 77) || (x >= 97 && x <= 109) ? (char)(x + 13) : (char)(x - 13) : x))), OpenReportInBrowser = true } }; _reportCrash.RetryFailedReports(); }
        public static void SendReport(Exception ex, string msg = "") { _reportCrash.DeveloperMessage = msg; _reportCrash.Silent = false; _reportCrash.Send(ex); }
        public static void SendReportSilently(Exception ex, string msg = "") { _reportCrash.DeveloperMessage = msg; _reportCrash.Silent = true; _reportCrash.Send(ex); }

        // OnSourceInitialized - Register hotkeys (Remove VK_MINUS)
        protected override void OnSourceInitialized(EventArgs e) { base.OnSourceInitialized(e); _windowHandle = new WindowInteropHelper(this).Handle; _source = HwndSource.FromHwnd(_windowHandle); _source.AddHook(HwndHook); string err = ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_I) ? "I," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_C) ? "C," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_1) ? "1," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_2) ? "2," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_3) ? "3," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_4) ? "4," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_5) ? "5," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_6) ? "6," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_7) ? "7," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_8) ? "8," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_9) ? "9," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_0) ? "0," : ""; /*VK_MINUS removed*/ err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_PLUS) ? "+," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_UP) ? "UP," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_DOWN) ? "DOWN," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_O) ? "O," : ""; if (!err.Equals("")) MessageBox.Show("Cannot Bind: " + err.Trim(','), "BIND ERROR"); }


        // HwndHook - Handle hotkey presses
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                int vkey = (((int)lParam >> 16) & 0xFFFF);

                // Always allow Attach attempt (VK_I)
                if (vkey != VK_I && !hook.hooked)
                {
                    Dispatcher.BeginInvoke((Action)(() => {
                        statusText.Foreground = System.Windows.Media.Brushes.Red;
                        statusText.Text = $"Not attached to {TargetProcessName}. Press Ctrl+Alt+I.";
                    }));
                    handled = true;
                    return IntPtr.Zero;
                }


                switch ((uint)vkey)
                {
                    // --- Sorting Modes (Update text to match HP1) ---
                    case VK_0: sortingMode = 10; draw.highLightMode("0 - Sexuality First", rightTextBox, leftTextBox); updateResultView(results); break; // Sexuality
                    case VK_1: sortingMode = 1; draw.highLightMode("1 - Chain First", leftTextBox, rightTextBox); updateResultView(results); selectedIndex = 0; if (resultListView.Items.Count > 0) resultListView.SelectedIndex = 0; break; // Chain
                    case VK_2: sortingMode = 2; draw.highLightMode("2 - Net Score First", leftTextBox, rightTextBox); updateResultView(results); break; // Total Net Score (TotalWBroken)
                    case VK_3: sortingMode = 3; draw.highLightMode("3 - 4/5 Match First", leftTextBox, rightTextBox); updateResultView(results); break; // Amount (4/5 match)
                    case VK_4: sortingMode = 4; draw.highLightMode("4 - Passion First", leftTextBox, rightTextBox); updateResultView(results); break; // Passion (Heart)
                    case VK_5: sortingMode = 5; draw.highLightMode("5 - Joy First", leftTextBox, rightTextBox); updateResultView(results); break; // Joy (Bell)
                    case VK_6: sortingMode = 6; draw.highLightMode("6 - Sentiment First", rightTextBox, leftTextBox); updateResultView(results); break; // Sentiment (Teardrop)
                    case VK_7: sortingMode = 7; draw.highLightMode("7 - Talent First", rightTextBox, leftTextBox); updateResultView(results); break; // Talent (Blue)
                    case VK_8: sortingMode = 8; draw.highLightMode("8 - Flirtation First", rightTextBox, leftTextBox); updateResultView(results); break; // Flirtation (Green)
                    case VK_9: sortingMode = 9; draw.highLightMode("9 - Romance First", rightTextBox, leftTextBox); updateResultView(results); break; // Romance (Orange)
                    case VK_PLUS: sortingMode = 12; draw.highLightMode("+ - Broken Heart First", rightTextBox, leftTextBox); updateResultView(results); break; // Broken Heart
                    // case VK_MINUS: // Removed as there's no direct equivalent for Stamina sort

                    // --- Scrolling (Overlay drawing commented out for now) ---
                    case VK_UP:
                        if (results != null && selectedIndex > 0)
                        {
                            selectedIndex--;
                            resultListView.SelectedIndex = selectedIndex;
                            resultListView.ScrollIntoView(resultListView.Items.GetItemAt(selectedIndex));
                            // If overlays are desired later, uncomment and ensure calibration:
                            DrawSelectedOverlay();
                        }
                        break;
                    case VK_DOWN:
                        if (results != null && selectedIndex < results.Count - 1)
                        {
                            selectedIndex++;
                            resultListView.SelectedIndex = selectedIndex;
                            resultListView.ScrollIntoView(resultListView.Items.GetItemAt(selectedIndex));
                            // If overlays are desired later, uncomment and ensure calibration:
                            DrawSelectedOverlay();
                        }
                        break;

                    // --- Other Keys ---
                    case VK_O: break; // Unused, keep available

                    case VK_I: // Attach/Re-attach
                        killIt = true; // Signal listener thread to stop trying (if running)
                        Dispatcher.BeginInvoke((Action)(() => {
                            statusText.Foreground = System.Windows.Media.Brushes.DarkOrange;
                            statusText.Text = "Attempting manual attach...";
                        }));
                        // Perform attach on a background thread to avoid blocking UI
                        new Thread(() => {
                            Thread.Sleep(200); // Small delay
                            hook.AttachProcess();
                            // After attach attempt, restart listener if needed
                            killIt = false; // Allow listener to restart
                            if (!hook.hooked) LaunchGameListener(); // Restart if attach failed
                        })
                        { IsBackground = true }.Start();
                        break;

                    case VK_C: // Capture and Parse (Solving Disabled for now)
                        bool canCapture = false;
                        if (hook.hooked && hook.processId != 0)
                        {
                            try
                            {
                                using (var p = Process.GetProcessById(hook.processId))
                                    canCapture = p != null && !p.HasExited;
                            }
                            catch { /* Process likely exited */ }
                        }

                        if (!canCapture)
                        {
                            Dispatcher.BeginInvoke((Action)(() => {
                                statusText.Foreground = System.Windows.Media.Brushes.Red;
                                statusText.Text = $"Cannot capture: Not attached or {TargetProcessName} not running.";
                            }));
                            break; // Exit case VK_C
                        }

                        // Proceed with capture
                        Dispatcher.BeginInvoke((Action)(() => {
                            statusText.Foreground = System.Windows.Media.Brushes.Blue; // Use full namespace
                            statusText.Text = debugMode ? "Parsing & Creating Debug Mask..." : "Capturing & Parsing Board...";
                        }));

                        new Thread(() => {
                            System.Drawing.Bitmap screenshot = captureBoard(); // Get screenshot using System.Drawing.Bitmap type
                            if (screenshot == null)
                            {
                                Dispatcher.BeginInvoke((Action)(() => {
                                    statusText.Foreground = System.Windows.Media.Brushes.Red;
                                    statusText.Text = "Capture failed (Screenshot is null).";
                                }));
                                return; // Exit thread if capture failed
                            }

                            // --- Use screenshot in a using block to ensure disposal ---
                            using (screenshot)
                            {
                                lastScreenHeight = screenshot.Height;
                                lastScreenWidth = screenshot.Width;
                                bool createMask = debugMode; // Use the class-level debugMode flag

                                // Call parseImage - it now handles cloning internally if creating mask
                                int[][] parsedBoard = solver.parseImage(screenshot, createMask);

                                // Update UI on the main thread
                                Dispatcher.BeginInvoke((Action)(() => {
                                    draw.drawBoard(parsedBoard); // Update the UI grid display

                                    if (createMask)
                                    {
                                        statusText.Foreground = System.Windows.Media.Brushes.DarkMagenta;
                                        statusText.Text = $"Board parsed. Debug mask saved.";
                                    }
                                    else
                                    {
                                        statusText.Foreground = System.Windows.Media.Brushes.MediumPurple; // Change color slightly
                                        statusText.Text = "Board Parsed. Solving..."; // Indicate solving starts
                                    }

                                    // --- ENABLE SOLVING AND RESULTS VIEW ---
                                    results.Clear();
                                    results = solver.loopBoard(parsedBoard); // Call the implemented loopBoard
                                    updateResultView(results, lastScreenHeight, lastScreenWidth); // Update the list view
                                                                                                  // --- END ENABLED SECTION ---

                                    resultListView.IsEnabled = results != null && results.Count > 0; // Enable list only if results exist

                                    if (!createMask)
                                    {
                                        statusText.Foreground = System.Windows.Media.Brushes.Green;
                                        if (results.Count > 0)
                                        {
                                            statusText.Text = $"Done! Best move highlighted in list ({results.Count} total).";
                                        }
                                        else
                                        {
                                            statusText.Text = "Done! No possible moves found.";
                                        }
                                    }
                                }));

                            } // screenshot is disposed here automatically by 'using'

                            GC.Collect(); // Optional: Force garbage collection

                        })
                        { IsBackground = true }.Start();
                        break; // End case VK_C
                }
                handled = true; // Mark the hotkey as handled
            }
            return IntPtr.Zero;
        }
        private void DrawSelectedOverlay()
        {
            // --- OPTIONAL: Comment out contents for PURE Option 1 ---
            if (hook.hooked && results != null && results.Count > selectedIndex && selectedIndex >= 0)
            {
                var selectedMove = results[selectedIndex];
                if (selectedMove.yPos >= 0 && selectedMove.yPos < board.Length &&
                    selectedMove.xPos >= 0 && selectedMove.xPos < board[selectedMove.yPos].Length)
                {
                    int tileColorIndex = board[selectedMove.yPos][selectedMove.xPos];
                    // Use the Text Overlay version for now if uncommented
                    var overlay = draw.parseMovementAndDraw(selectedMove, tileColorIndex, lastScreenHeight, lastScreenWidth);
                    hook.drawOverlay(overlay);
                    Console.WriteLine($"DrawSelectedOverlay: Drawing text overlay for index {selectedIndex}.");
                }
                else { Console.WriteLine($"[WARN] DrawSelectedOverlay: Invalid board coordinates for index {selectedIndex}"); }
            }
            else if (hook.hooked)
            {
                hook.drawOverlay(new Capture.Hook.Common.Overlay { Elements = new List<Capture.Hook.Common.IOverlayElement>(), Hidden = true }); // Clear overlay
                Console.WriteLine($"DrawSelectedOverlay: Clearing overlay.");
            }
            // --- END OPTIONAL ---
        }

        // OnClosed remains the same
        protected override void OnClosed(EventArgs e) { if (_source != null) { _source.RemoveHook(HwndHook); _source = null; } UnregisterHotKey(_windowHandle, HOTKEY_ID); killIt = true; base.OnClosed(e); }

        // captureBoard - Uses System.Drawing.Bitmap
        private System.Drawing.Bitmap captureBoard() { return hook.getScreenshot(); }

        // updateResultView methods remain the same, but rely on resultItem/SolverInterface changes
        private void updateResultView(List<SolverInterface.Movement> res) { updateResultView(res, lastScreenHeight, lastScreenWidth); }
        private void updateResultView(List<SolverInterface.Movement> inList, int h, int w)
        {
            // Clear previous results
            resultListView.Items.Clear();

            if (inList == null || inList.Count == 0) // Handle null or empty list gracefully
            {
                results = new List<SolverInterface.Movement>(); // Ensure results is an empty list, not null
                selectedIndex = -1; // No item to select
                resultListView.IsEnabled = false;
                Console.WriteLine("updateResultView: No results to display.");
                return; // Nothing more to do
            }

            // Sort the list to get the best move at index 0
            // Ensure sortList returns a valid list, even if empty (though we checked above)
            results = solver.sortList(inList, sortingMode) ?? new List<SolverInterface.Movement>();
            Console.WriteLine($"updateResultView: Sorted {results.Count} results using mode {sortingMode}.");

            // Populate the ListView
            results.ForEach(r => resultListView.Items.Add(new resultItem(r)));

            // --- OPTION 1 IMPLEMENTATION ---
            if (results.Count > 0)
            {
                selectedIndex = 0;                     // Set internal index tracking the selection
                resultListView.SelectedIndex = 0;      // Select the first item visually in the list
                resultListView.ScrollIntoView(resultListView.Items.GetItemAt(0)); // Scroll the view to the selected item
                resultListView.IsEnabled = true;       // Enable the list for interaction
                Console.WriteLine($"updateResultView: Selected and scrolled to index 0.");

                // --- CRITICAL: REMOVE or COMMENT OUT the automatic overlay call for the best move ---
                // hook.drawOverlay(draw.parseMovementAndDraw(results[0], board[results[0].yPos][results[0].xPos], h, w));
                // Console.WriteLine($"updateResultView: Skipped automatic overlay draw for Option 1."); // Optional log
            }
            else // Should not happen due to earlier check, but good practice
            {
                selectedIndex = -1;
                resultListView.IsEnabled = false;
                Console.WriteLine("updateResultView: Results list became empty after processing.");
            }
        }

        // resultListView_SelectionChanged remains the same
        private void resultListView_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    } // End Class
} // End Namespace