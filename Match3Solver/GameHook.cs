// FILE: Match3Solver/GameHook.cs
using Capture.Hook;
using Capture;
using System;
using System.Diagnostics;
using Capture.Interface;
using System.Threading;
using System.Windows.Controls;

using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using System.Windows.Media;

namespace Match3Solver
{
    public class GameHook
    {
        // --- MODIFICATION: Make processId public ---
        public int processId = 0;
        // --- END MODIFICATION ---
        internal Process _process; // Keep internal or private
        internal CaptureProcess _captureProcess; // Keep internal or private
        public Boolean hooked = false;

        TextBlock message; // The TextBlock from MainWindow to display status
        MainWindow parent; // Reference to the MainWindow

        private const string TargetProcessName = "HuniePop";

        private Thread sDX = null;

        public GameHook(TextBlock statusMessage, MainWindow window)
        {
            this.message = statusMessage;
            this.parent = window;
        }

        // FILE: Match3Solver/GameHook.cs (Modify AttachProcess method)

        public void AttachProcess()
        {
            parent.Dispatcher.BeginInvoke((Action)(() =>
            {
                message.Foreground = new SolidColorBrush(Colors.Orange);
                message.Text = $"Searching for '{TargetProcessName}' process...";
            }));
            Thread.Sleep(50);

            Process[] processes = Process.GetProcessesByName(TargetProcessName);
            bool processFound = false;
            _captureProcess = null; // Ensure it's null at the start of the attempt

            foreach (Process process in processes)
            {
                processFound = true;

                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        message.Foreground = new SolidColorBrush(Colors.OrangeRed);
                        message.Text = $"Found '{TargetProcessName}' (PID: {process.Id}) but it has no window handle yet. Skipping.";
                    }));
                    continue;
                }

                if (HookManager.IsHooked(process.Id))
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        message.Foreground = new SolidColorBrush(Colors.Blue);
                        message.Text = $"'{TargetProcessName}' (PID: {process.Id}) is already hooked. Skipping.";
                    }));
                    hooked = true;
                    _process = process;
                    processId = process.Id;
                    return;
                }

                parent.Dispatcher.BeginInvoke((Action)(() =>
                {
                    message.Foreground = new SolidColorBrush(Colors.BlueViolet);
                    message.Text = $"Found '{TargetProcessName}' (PID: {process.Id}). Pausing before injection...";
                }));
                Thread.Sleep(1500); // Wait 1.5 seconds in case game is still initializing

                Direct3DVersion direct3DVersion = Direct3DVersion.AutoDetect;
                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = true // Keep true for now, simplify later if needed
                };

                _process = process; // Assign _process before try block

                try
                {
                    processId = process.Id;
                    var captureInterface = new CaptureInterface();
                    captureInterface.RemoteMessage += new MessageReceivedEvent(CaptureInterface_RemoteMessage);

                    // --- ADDED LOGGING ---
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] PRE-INJECT: Attempting to create CaptureProcess for PID: {processId}...");
                    parent.Dispatcher.BeginInvoke((Action)(() => { message.Text = $"Injecting into '{TargetProcessName}' (PID: {processId})..."; }));
                    // --- END LOGGING ---

                    // This line includes the call to EasyHook's RemoteHooking.Inject
                    _captureProcess = new CaptureProcess(process, cc, captureInterface);

                    // --- ADDED LOGGING ---
                    // If we reach here, the constructor and RemoteHooking.Inject call *completed* without throwing an exception *in the solver process*.
                    // The injected code might still fail later, preventing communication.
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] POST-INJECT: CaptureProcess creation constructor finished for PID: {processId}.");
                    // --- END LOGGING ---

                    break; // Exit loop on successful CaptureProcess constructor return
                }
                catch (ProcessAlreadyHookedException ex) // Specific catch
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] INJECT-SKIP: ProcessAlreadyHookedException for PID: {process.Id}");
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        message.Foreground = new SolidColorBrush(Colors.Blue);
                        message.Text = $"'{TargetProcessName}' (PID: {process.Id}) was already hooked (detected during injection). Skipping.";
                    }));
                    hooked = true;
                    processId = process.Id;
                    return; // Exit method
                }
                catch (Exception ex) // Catch ANY other exception during injection setup
                {
                    // --- ADDED LOGGING ---
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] INJECT-FAIL: Injection Exception for PID: {process.Id}: {ex.ToString()}"); // Log full exception
                                                                                                                                                 // --- END LOGGING ---
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        message.Foreground = new SolidColorBrush(Colors.Red);
                        message.Text = $"Failed to inject into '{TargetProcessName}' (PID: {process.Id}): {ex.GetType().Name}";
                    }));
                    _captureProcess = null; // Ensure it's null on failure
                    processId = 0;
                    // Continue loop to try next process if any (though usually there's only one)
                }
            }
            Thread.Sleep(10); // Short pause after loop

            // --- Re-evaluate status AFTER the loop ---
            if (_captureProcess != null) // Injection constructor succeeded
            {
                // --- ADDED LOGGING ---
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ATTACH-SUCCESS: CaptureProcess object created. Setting status for PID: {processId}.");
                // --- END LOGGING ---
                parent.Dispatcher.BeginInvoke((Action)(() => {
                    message.Foreground = new SolidColorBrush(Colors.DarkGreen);
                    message.Text = $"Successfully attached to '{TargetProcessName}' (PID: {processId})";
                }));
                hooked = true;
                // processId was set in the try block
            }
            else if (!hooked) // If captureProcess is null AND we didn't skip due to already being hooked
            {
                // --- ADDED LOGGING ---
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ATTACH-FAIL: CaptureProcess is null and not already hooked.");
                // --- END LOGGING ---
                if (processFound)
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        message.Foreground = new SolidColorBrush(Colors.OrangeRed);
                        message.Text = $"Found '{TargetProcessName}', but failed to attach (check console/logs).";
                    }));
                }
                // If process was not found at all, the listener will show the 'waiting' message when restarted.
                this.parent.LaunchGameListener(); // Restart listener
                hooked = false;
                processId = 0;
            }
            // If _captureProcess is null but hooked is true, we already handled the status message and returned.
        }

        // Rest of GameHook.cs remains the same...
        // Remember the CaptureInterface_RemoteMessage method prints messages received *back* from the injected code.
        void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            // --- ADDED LOGGING ---
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] HOOK-MSG: Received message: {message.MessageType}: {message.Message}");
            // --- END LOGGING ---

            // Example: Update UI status based on hook messages if needed (can be helpful for debugging hook init)
            // parent.Dispatcher.BeginInvoke((Action)(() => {
            //     // Maybe change color based on message.MessageType
            //     // Be cautious about flooding UI
            //     message.Text = $"Hook Status: {message.Message}";
            // }));
        }

        public Bitmap getScreenshot()
        {
            if (!hooked || _captureProcess == null)
            {
                parent.Dispatcher.BeginInvoke((Action)(() =>
                {
                    message.Foreground = new SolidColorBrush(Colors.Red);
                    message.Text = "Error: Not attached to game for screenshot.";
                }));
                return null;
            }
            try
            {
                // Bring to front might fail if the process exited unexpectedly
                _captureProcess.BringProcessWindowToFront();
                return _captureProcess.CaptureInterface.GetScreenshot(Rectangle.Empty, new TimeSpan(0, 0, 3), null, ImageFormat.Png).ToBitmap();
            }
            catch (Exception ex)
            {
                parent.Dispatcher.BeginInvoke((Action)(() =>
                {
                    message.Foreground = new SolidColorBrush(Colors.Red);
                    message.Text = $"Error capturing screenshot: {ex.Message}";
                }));
                return null;
            }
        }

        public void drawOverlay(Capture.Hook.Common.Overlay items)
        {
            if (hooked && _captureProcess != null)
            {
                try
                {
                    _captureProcess.CaptureInterface.DrawOverlayInGame(items);
                }
                catch (Exception ex)
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        message.Foreground = new SolidColorBrush(Colors.Red);
                        message.Text = $"Error drawing overlay: {ex.Message}";
                    }));
                }
            }
        }
    }
}