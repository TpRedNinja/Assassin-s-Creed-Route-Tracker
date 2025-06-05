using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assassin_s_Creed_Route_Tracker.Properties;

namespace Assassin_s_Creed_Route_Tracker
{
    public class GameConnectionManager
    {
        private IntPtr processHandle;
        private IntPtr baseAddress;
        private string currentProcess = string.Empty;
        private GameStatsBase? gameStats;

        private const int PROCESS_WM_READ = 0x0010;

        // Forward the StatsUpdated event from GameStats
        public event EventHandler<StatsUpdatedEventArgs>? StatsUpdated;

        public bool IsConnected => processHandle != IntPtr.Zero && baseAddress != IntPtr.Zero;
        public GameStatsBase? GameStats => gameStats;
        public IntPtr ProcessHandle => processHandle;
        public IntPtr BaseAddress => baseAddress;

        // ==========FORMAL COMMENT=========
        // Establishes connection to the game process
        // Finds the specified process, gets handle and base address for memory access
        // ==========MY NOTES==============
        // Finds the game in running processes and gets access to its memory
        // Sets up everything needed to read values from the game
        [SupportedOSPlatform("windows6.1")]
        public void Connect()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(currentProcess.Replace(".exe", ""));
                if (processes.Length > 0)
                {
                    Process process = processes[0];
                    processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
                    
                    // Add null check for MainModule
                    if (process.MainModule != null)
                    {
                        baseAddress = process.MainModule.BaseAddress;
                        Debug.WriteLine($"Connected to process {currentProcess}");
                        Debug.WriteLine($"Base address: {baseAddress:X}");
                    }
                    else
                    {
                        processHandle = IntPtr.Zero;
                        baseAddress = IntPtr.Zero;
                        Debug.WriteLine($"Cannot access MainModule for process {currentProcess}. This may be due to insufficient permissions.");
                    }
                }
                else
                {
                    processHandle = IntPtr.Zero;
                    baseAddress = IntPtr.Zero;
                    Debug.WriteLine($"Process {currentProcess} not found.");
                }
            }
            catch (Exception ex)
            {
                processHandle = IntPtr.Zero;
                baseAddress = IntPtr.Zero;
                Debug.WriteLine($"Error in Connect: {ex.Message}");
            }
        }

        // ==========FORMAL COMMENT=========
        // Checks if a specific process is currently running
        // Returns true if the process is found, false otherwise
        // ==========MY NOTES==============
        // Just checks if the game is already running or not
        [SupportedOSPlatform("windows6.1")]
        public bool IsProcessRunning(string processName)
        {
            return Process.GetProcessesByName(processName.Replace(".exe", "")).Length > 0;
        }

        // ==========FORMAL COMMENT=========
        // Launches the specified game executable from its directory
        // Handles cases where directory or executable file is not found
        // ==========MY NOTES==============
        // Tries to start the game and shows error messages if it can't
        [SupportedOSPlatform("windows6.1")]
        public void StartGame(string processName)
        {
            try
            {
                string gameDirectory = string.Empty;
                if (currentProcess == "AC4BFSP.exe")
                {
                    gameDirectory = Settings.Default?.AC4Directory ?? string.Empty;
                }
                else if (currentProcess == "GoW.exe")
                {
                    gameDirectory = Settings.Default?.GoW2018Directory ?? string.Empty;
                }

                if (string.IsNullOrEmpty(gameDirectory))
                {
                    MessageBox.Show("Please select the game's directory.");
                    return;
                }

                string gamePath = System.IO.Path.Combine(gameDirectory, processName);
                if (!System.IO.File.Exists(gamePath))
                {
                    MessageBox.Show($"The game executable was not found in the selected directory: {gamePath}");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = gamePath,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting the game: {ex.Message}");
            }
        }

        // ==========FORMAL COMMENT=========
        // Waits for the game process to start with a timeout
        // Shows custom dialog with options if game fails to start within time limit
        // ==========MY NOTES==============
        // Waits up to 10 seconds for the game to start
        // If it takes too long, gives you options like retry, wait longer, etc.
        [SupportedOSPlatform("windows6.1")]
        public async Task WaitForGameToStartAsync(int recursionCount = 0)
        {
            // Add recursion limit to prevent stack overflow
            if (recursionCount > 3)
            {
                MessageBox.Show("Maximum retry count reached. Please start the game manually.");
                return;
            }

            Stopwatch stopwatch = new();
            stopwatch.Start();

            while (stopwatch.Elapsed.TotalSeconds < 10)
            {
                if (IsProcessRunning(currentProcess))
                {
                    return;
                }

                await Task.Delay(1000);
            }

            stopwatch.Stop();

            using CustomMessageBox customMessageBox = new("The game did not start within 10 seconds. Would you like to try again, wait another 10 seconds, manually start the game, or cancel?");
            if (customMessageBox.ShowDialog() == DialogResult.OK)
            {
                switch (customMessageBox.Result)
                {
                    case CustomMessageBox.CustomDialogResult.TryAgain:
                        StartGame(currentProcess);
                        await WaitForGameToStartAsync(recursionCount + 1);
                        break;
                    case CustomMessageBox.CustomDialogResult.Wait:
                        await WaitForGameToStartAsync();
                        break;
                    case CustomMessageBox.CustomDialogResult.Manually:
                        // Do nothing, user chose to manually start the game
                        break;
                    case CustomMessageBox.CustomDialogResult.Cancel:
                        // Do nothing, user chose to cancel
                        break;
                }
            }
        }

        public void SetCurrentProcess(string processName)
        {
            currentProcess = processName;
        }

        public void InitializeGameStats(string gameName)
        {
            if (processHandle != IntPtr.Zero && baseAddress != IntPtr.Zero)
            {
                bool is64Bit = IsTargetProcess64Bit();
                Debug.WriteLine($"Initializing GameStats for {(is64Bit ? "64-bit" : "32-bit")} process");

                // Create the correct stats object based on game
                gameStats = gameName switch
                {
                    "Assassin's Creed 4" => new AC4GameStats(processHandle, baseAddress, is64Bit),
                    "God of War 2018" => new GoW2018GameStats(processHandle, baseAddress, is64Bit),
                    _ => throw new NotSupportedException($"Game {gameName} is not supported")
                };

                gameStats.StatsUpdated += OnGameStatsUpdated;
                gameStats.StartUpdating();
            }
        }

        // In OnGameStatsUpdated - already correct with ?. operator
        private void OnGameStatsUpdated(object? sender, StatsUpdatedEventArgs e)
        {
            StatsUpdated?.Invoke(this, e);
        }

        public void CleanupGameStats()
        {
            if (gameStats != null)
            {
                gameStats.StatsUpdated -= OnGameStatsUpdated;
                gameStats.StopUpdating();
            }
        }

        // ==========FORMAL COMMENT=========
        // High-level method that orchestrates the game connection process
        // Handles game selection, auto-starting if needed, and statistics initialization
        // ==========MY NOTES==============
        // One-stop method for connecting to a game - handles all the connection steps
        // Returns success/failure so the UI can update accordingly
        [SupportedOSPlatform("windows6.1")]
        public async Task<bool> ConnectToGameAsync(string gameName, bool autoStart = false)
        {
            // Set the correct process name based on game selection
            if (gameName == "Assassin's Creed 4")
                currentProcess = "AC4BFSP.exe";
            else if (gameName == "God of War 2018")
                currentProcess = "GoW.exe";
            else
                return false; // Invalid game selection

            // Auto-start the game if requested and not already running
            if (autoStart)
            {
                if (gameName == "God of War 2018")
                    return false; // Auto-start not supported for Syndicate

                if (!IsProcessRunning(currentProcess))
                {
                    StartGame(currentProcess);
                    await WaitForGameToStartAsync();
                }
            }

            // Attempt to connect to the game process
            Connect();

            // Initialize game stats if connection was successful
            if (processHandle != IntPtr.Zero)
            {
                InitializeGameStats(gameName);
                return true;
            }

            return false;
        }

        public bool IsTargetProcess64Bit()
        {
            if (processHandle == IntPtr.Zero)
                return Environment.Is64BitOperatingSystem; // Default assumption
            
            // Check if process is 64-bit
            if (!IsWow64Process(processHandle, out bool isWow64Process))
                return Environment.Is64BitOperatingSystem;
            
            // On 64-bit Windows: 
            // - 32-bit processes run under WOW64 (isWow64Process = true)
            // - 64-bit processes do not (isWow64Process = false)
            return Environment.Is64BitOperatingSystem && !isWow64Process;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    }
}
