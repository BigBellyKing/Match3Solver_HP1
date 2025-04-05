// FILE: Match3Solver/MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Documents;
using CrashReporterDotNET;
using System.Text;
using System.Linq;
using System.Diagnostics;
using Capture.Hook;
using Capture.Interface;
using System.IO; // Keep for Path ambiguity resolution

namespace Match3Solver
{
    public partial class MainWindow : Window, SolverInterface
    {
        public int width = 8; public int length = 7; public int boxSize = 30; public int sortingMode = 1;
        public int[][] board = new int[7][]; public Rectangle[][] boardDisplay = new Rectangle[7][];
        List<SolverInterface.Movement> results; public SolverUtils solver; public GameHook hook; public UIFunctions draw;
        // --- SET debugMode = true TO CREATE MASKS ---
        public Boolean debugMode = true; // CHANGE THIS TO false FOR NORMAL OPERATION
        // --- END DEBUG MODE ---
        private int lastScreenHeight = 0; private int lastScreenWidth = 0; private int selectedIndex = 0;
        private const string TargetProcessName = "HuniePop";
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr FindWindow(string n, string w);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr h, int id, uint m, uint k);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr h, int id);
        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0, MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_WIN = 8;
        private const uint VK_I = 0x49, VK_C = 0x43, VK_0 = 0x30, VK_1 = 0x31, VK_2 = 0x32, VK_3 = 0x33, VK_4 = 0x34, VK_5 = 0x35, VK_6 = 0x36, VK_7 = 0x37, VK_8 = 0x38, VK_9 = 0x39, VK_PLUS = 0xBB, VK_MINUS = 0xBD, VK_UP = 0x26, VK_DOWN = 0x28, VK_O = 0x4F;
        private IntPtr _windowHandle; private HwndSource _source; private static ReportCrash _reportCrash;
        private Thread waitLooper; private Boolean killIt = false;

        public MainWindow()
        {
            InitializeComponent(); statusText.Text = $"Waiting for {TargetProcessName}."; LaunchGameListener();
            hook = new GameHook(statusText, this); solver = new SolverUtils(length, width, boardDisplay); draw = new UIFunctions(this);
            for (int y = 0; y < length; y++) { board[y] = new int[width]; for (int x = 0; x < width; x++) board[y][x] = 9; }
            draw.initBoardDisplay(); results = new List<SolverInterface.Movement>(); initCrashReporter();
        }

        public void LaunchGameListener() { new Thread(() => { while (true) { if (killIt) break; bool g = false; try { g = Process.GetProcessesByName(TargetProcessName).Length > 0; } catch { } if (g) { Dispatcher.BeginInvoke((Action)(() => { if (!hook.hooked) hook.AttachProcess(); })); break; } else { Dispatcher.BeginInvoke((Action)(() => { if (!hook.hooked) { statusText.Foreground = Brushes.IndianRed; statusText.Text = $"Waiting for {TargetProcessName} to Open."; } })); } Thread.Sleep(2000); } }) { IsBackground = true, Name = "GameListenerThread" }.Start(); }
        private static void initCrashReporter() { AppDomain.CurrentDomain.UnhandledException += (s, a) => SendReport((Exception)a.ExceptionObject); byte[] d1 = Convert.FromBase64String("enFhYy56cWh1K3puZ3B1M2ZieWlyZXBlbmZ1ZXJjYmVnQHR6bnZ5LnBieg=="); string s1 = Encoding.UTF8.GetString(d1); byte[] d2 = Convert.FromBase64String("cjBwOW9vcHEtNzVzcC00cHJxLW8yNjktbnIzMTQ5MDc0OTQy"); string s2 = Encoding.UTF8.GetString(d2); _reportCrash = new ReportCrash(String.Join("", s1.Select(x => char.IsLetter(x) ? (x >= 65 && x <= 77) || (x >= 97 && x <= 109) ? (char)(x + 13) : (char)(x - 13) : x))) { Silent = true, ShowScreenshotTab = true, IncludeScreenshot = false, AnalyzeWithDoctorDump = true, DoctorDumpSettings = new DoctorDumpSettings { ApplicationID = new Guid(String.Join("", s2.Select(x => char.IsLetter(x) ? (x >= 65 && x <= 77) || (x >= 97 && x <= 109) ? (char)(x + 13) : (char)(x - 13) : x))), OpenReportInBrowser = true } }; _reportCrash.RetryFailedReports(); }
        public static void SendReport(Exception ex, string msg = "") { _reportCrash.DeveloperMessage = msg; _reportCrash.Silent = false; _reportCrash.Send(ex); }
        public static void SendReportSilently(Exception ex, string msg = "") { _reportCrash.DeveloperMessage = msg; _reportCrash.Silent = true; _reportCrash.Send(ex); }

        protected override void OnSourceInitialized(EventArgs e) { base.OnSourceInitialized(e); _windowHandle = new WindowInteropHelper(this).Handle; _source = HwndSource.FromHwnd(_windowHandle); _source.AddHook(HwndHook); string err = ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_I) ? "I," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_C) ? "C," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_1) ? "1," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_2) ? "2," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_3) ? "3," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_4) ? "4," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_5) ? "5," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_6) ? "6," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_7) ? "7," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_8) ? "8," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_9) ? "9," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_0) ? "0," : ""; /*VK_MINUS removed*/ err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_PLUS) ? "+," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_UP) ? "UP," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_DOWN) ? "DOWN," : ""; err += !RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_O) ? "O," : ""; if (!err.Equals("")) MessageBox.Show("Cannot Bind: " + err.Trim(','), "BIND ERROR"); }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                int vkey = (((int)lParam >> 16) & 0xFFFF);
                if (!hook.hooked && vkey != VK_I) { Dispatcher.BeginInvoke((Action)(() => { statusText.Foreground = Brushes.Red; statusText.Text = $"Not attached to {TargetProcessName}. Press Ctrl+Alt+I."; })); handled = true; return IntPtr.Zero; }
                switch ((uint)vkey)
                {
                    case VK_0: sortingMode = 10; draw.highLightMode("0 - Sexuality First", rightTextBox, leftTextBox); updateResultView(results); break;
                    case VK_1: sortingMode = 1; draw.highLightMode("1 - Chain First", leftTextBox, rightTextBox); updateResultView(results); selectedIndex = 0; if (resultListView.Items.Count > 0) resultListView.SelectedIndex = 0; break;
                    case VK_2: sortingMode = 2; draw.highLightMode("2 - Net Score First", leftTextBox, rightTextBox); updateResultView(results); break;
                    case VK_3: sortingMode = 3; draw.highLightMode("3 - 4/5 Match First", leftTextBox, rightTextBox); updateResultView(results); break;
                    case VK_4: sortingMode = 4; draw.highLightMode("4 - Passion First", leftTextBox, rightTextBox); updateResultView(results); break;
                    case VK_5: sortingMode = 5; draw.highLightMode("5 - Joy First", leftTextBox, rightTextBox); updateResultView(results); break;
                    case VK_6: sortingMode = 6; draw.highLightMode("6 - Sentiment First", rightTextBox, leftTextBox); updateResultView(results); break;
                    case VK_7: sortingMode = 7; draw.highLightMode("7 - Talent First", rightTextBox, leftTextBox); updateResultView(results); break;
                    case VK_8: sortingMode = 8; draw.highLightMode("8 - Flirtation First", rightTextBox, leftTextBox); updateResultView(results); break;
                    case VK_9: sortingMode = 9; draw.highLightMode("9 - Romance First", rightTextBox, leftTextBox); updateResultView(results); break;
                    case VK_PLUS: sortingMode = 12; draw.highLightMode("+ - Broken Heart First", rightTextBox, leftTextBox); updateResultView(results); break;
                    case VK_UP: if (results != null && selectedIndex > 0) { selectedIndex--; resultListView.SelectedIndex = selectedIndex; resultListView.ScrollIntoView(resultListView.Items.GetItemAt(selectedIndex)); /* hook.drawOverlay(...) */ } break;
                    case VK_DOWN: if (results != null && selectedIndex < results.Count - 1) { selectedIndex++; resultListView.SelectedIndex = selectedIndex; resultListView.ScrollIntoView(resultListView.Items.GetItemAt(selectedIndex)); /* hook.drawOverlay(...) */ } break;
                    case VK_O: break;
                    case VK_I: killIt = true; Dispatcher.BeginInvoke((Action)(() => { statusText.Foreground = Brushes.DarkOrange; statusText.Text = "Attempting attach..."; })); new Thread(() => { Thread.Sleep(200); hook.AttachProcess(); }) { IsBackground = true }.Start(); break;
                    case VK_C:
                        bool canCapture = false; if (hook.hooked && hook.processId != 0) { try { using (var p = Process.GetProcessById(hook.processId)) canCapture = p != null && !p.HasExited; } catch { } }
                        if (!canCapture) { Dispatcher.BeginInvoke((Action)(() => { statusText.Foreground = Brushes.Red; statusText.Text = $"Cannot capture: Not attached/running {TargetProcessName}."; })); break; }

                        Dispatcher.BeginInvoke((Action)(() => { statusText.Foreground = Brushes.Blue; statusText.Text = debugMode ? "Creating Debug Mask..." : "Capturing Screenshot..."; }));
                        new Thread(() => {
                            System.Drawing.Bitmap screenshot = captureBoard(); if (screenshot == null) return;
                            lastScreenHeight = screenshot.Height; lastScreenWidth = screenshot.Width;
                            bool createMask = debugMode;
                            int[][] parsedBoard;
                            // Clone the bitmap before passing if we need the original later
                            // Or just use the reference if parseImage handles cloning internally for mask saving
                            using (screenshot)
                            { // Ensure disposal
                                parsedBoard = solver.parseImage(screenshot, createMask);
                            } // screenshot disposed here
                            GC.Collect(); // Optional

                            Dispatcher.BeginInvoke((Action)(() => {
                                draw.drawBoard(parsedBoard); // Draw the parsed result on UI grid
                                if (createMask)
                                {
                                    // Use System.IO.Path explicitly
                                    string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "debug_mask_*.png");
                                    statusText.Foreground = Brushes.DarkMagenta;
                                    statusText.Text = $"Debug mask saved near {Environment.CurrentDirectory}";
                                }
                                else
                                {
                                    statusText.Foreground = Brushes.MediumPurple;
                                    statusText.Text = "Board Parsed (Solving Disabled)";
                                    // --- Solving/Results Disabled ---
                                    // results.Clear();
                                    // results = solver.loopBoard(parsedBoard); // Needs implementation
                                    // updateResultView(results, lastScreenHeight, lastScreenWidth);
                                    // --- End Disabled ---
                                }
                            }));
                        })
                        { IsBackground = true }.Start();
                        break;
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e) { if (_source != null) { _source.RemoveHook(HwndHook); _source = null; } UnregisterHotKey(_windowHandle, HOTKEY_ID); killIt = true; base.OnClosed(e); }
        private System.Drawing.Bitmap captureBoard() { return hook.getScreenshot(); }
        private void updateResultView(List<SolverInterface.Movement> res) { updateResultView(res, lastScreenHeight, lastScreenWidth); }
        private void updateResultView(List<SolverInterface.Movement> inList, int h, int w) { if (inList == null || inList.Count == 0) { resultListView.Items.Clear(); return; } resultListView.Items.Clear(); results = solver.sortList(inList, sortingMode); results.ForEach(r => resultListView.Items.Add(new resultItem(r))); selectedIndex = 0; if (resultListView.Items.Count > 0) { resultListView.SelectedIndex = 0; resultListView.ScrollIntoView(resultListView.Items.GetItemAt(selectedIndex)); /* hook.drawOverlay(...) */ } }
        private void resultListView_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    } // End Class
} // End Namespace