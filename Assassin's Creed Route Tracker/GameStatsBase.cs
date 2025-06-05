using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Route_Tracker
{
    // ==========FORMAL COMMENT=========
    // Event args class that carries game statistics data to event subscribers
    // Contains properties for all tracked game metrics to provide a complete snapshot
    // Used when notifying UI components about updated game statistics
    // ==========MY NOTES==============
    // This package contains all the stats in one neat bundle
    // When stats update, this gets sent to anyone listening for changes
    // Makes it easy to update the UI with all new values at once
    public class StatsUpdatedEventArgs(
    int percent,
    float percentFloat,
    int viewpoints,
    int myan,
    int treasure,
    int fragments,
    int assassin,
    int naval,
    int letters,
    int manuscripts,
    int music,
    int forts,
    int taverns,
    int totalChests) : EventArgs
    {
        public int Percent { get; } = percent;
        public float PercentFloat { get; } = percentFloat;
        public int Viewpoints { get; } = viewpoints;
        public int Myan { get; } = myan;
        public int Treasure { get; } = treasure;
        public int Fragments { get; } = fragments;
        public int Assassin { get; } = assassin;
        public int Naval { get; } = naval;
        public int Letters { get; } = letters;
        public int Manuscripts { get; } = manuscripts;
        public int Music { get; } = music;
        public int Forts { get; } = forts;
        public int Taverns { get; } = taverns;
        public int TotalChests { get; } = totalChests;
    }

    // ==========FORMAL COMMENT=========
    // Class that interfaces with game memory to read player statistics
    // Handles memory calculation, data extraction, and real-time updates for all tracked stats
    // Supports both individual collectibles and range-based collectible counting with auto-refreshing
    // ==========MY NOTES==============
    // This reads all the game stats from memory using addresses and offsets
    // It updates automatically every second to keep the UI in sync with the game
    // The core class that powers the entire tracking functionality
    public abstract unsafe class GameStatsBase : IGameStats
    {

        // Update constructor to accept architecture info
        public GameStatsBase(IntPtr processHandle, IntPtr baseAddress)
        {
            this.processHandle = processHandle;
            this.baseAddress = baseAddress;
        }

        // Change private fields to protected so derived classes can access them
        protected readonly IntPtr processHandle;
        protected readonly IntPtr baseAddress;

        // Timer-related fields for automatic stat updates
        private System.Threading.CancellationTokenSource? _updateCancellationTokenSource;
        private bool _isUpdating = false;
        private readonly int _updateIntervalMs = 1000; // Default update interval of 1 second
        private System.Threading.Timer? _updateTimer; // Timer that controls the periodic stat updates

        // Event that fires whenever game statistics change
        // UI components can subscribe to this to get real-time updates
        public event EventHandler<StatsUpdatedEventArgs>? StatsUpdated;

        // ==========FORMAL COMMENT=========
        // Generic method to read values from game memory
        // Follows chains of pointers using the provided offsets
        // ==========MY NOTES==============
        // This is the low-level function that actually reads from memory
        // It follows the trail of addresses to find the specific value we want
        // Simplify Read to just use the original method
        protected unsafe T Read<T>(nint baseAddress, int[] offsets) where T : unmanaged
        {
            nint address = baseAddress;
            Debug.WriteLine($"Reading memory at base address: {baseAddress:X}");

            int pointer_size = 4; // Always use 32-bit
            foreach (int offset in offsets)
            {
                if (!ReadProcessMemory(processHandle, address, &address, pointer_size, out nint bytesReads) ||
                    bytesReads != pointer_size || address == IntPtr.Zero)
                {
                    Debug.WriteLine($"Failed to read memory address at offset {offset:X}");
                    return default;
                }
                address += offset;
                Debug.WriteLine($"Address after applying offset {offset:X}: {address:X}");
            }

            T value;
            int size = sizeof(T);
            if (!ReadProcessMemory(processHandle, address, &value, size, out nint bytesRead) ||
                bytesRead != size)
            {
                Debug.WriteLine($"Failed to read value from memory at address {address:X}");
                return default;
            }

            Debug.WriteLine($"Read value from memory at address {address:X}: {value}");
            return value;
        }

        // Make GetStats() abstract so each game must implement it
        public abstract (int Percent, float PercentFloat, int Viewpoints, int Myan, int Treasure,
            int Fragments, int Assassin, int Naval, int Letters, int Manuscripts, int Music,
            int Forts, int Taverns, int TotalChests) GetStats();

        // Add the new interface method
        public virtual Dictionary<string, object> GetStatsAsDictionary()
        {
            var stats = GetStats();

            return new Dictionary<string, object>
            {
                ["Completion Percentage"] = stats.Percent,
                ["Exact Percentage"] = Math.Round(stats.PercentFloat, 2),
                ["Viewpoints Completed"] = stats.Viewpoints,
                ["Myan Stones Collected"] = stats.Myan,
                ["Buried Treasure Collected"] = stats.Treasure,
                ["AnimusFragments Collected"] = stats.Fragments,
                ["AssassinContracts Completed"] = stats.Assassin,
                ["NavalContracts Completed"] = stats.Naval,
                ["LetterBottles Collected"] = stats.Letters,
                ["Manuscripts Collected"] = stats.Manuscripts,
                ["Music Sheets Collected"] = stats.Music,
                ["Forts Captured"] = stats.Forts,
                ["Taverns unlocked"] = stats.Taverns,
                ["Total Chests Collected"] = stats.TotalChests
            };
        }

        // ==========FORMAL COMMENT=========
        // Timer-based auto-update system for game statistics
        // Periodically reads memory and notifies listeners when values change
        // Includes proper resource management and thread safety
        // ==========MY NOTES==============
        // Automatically refreshes stats every second without manual button clicks
        // Uses a timer instead of async/await to work properly in unsafe contexts
        // Makes sure everything gets cleaned up properly when we disconnect
        public void StartUpdating()
        {
            //dont start if already updating
            if (_isUpdating) return;

            // create cancellation token
            _updateCancellationTokenSource = new System.Threading.CancellationTokenSource();

            // mark as updating
            _isUpdating = true;

            // create and start timer
            _updateTimer = new System.Threading.Timer(UpdateCallback, null, 0, _updateIntervalMs);
        }

        // ==========FORMAL COMMENT=========
        // Callback method invoked by the update timer at regular intervals
        // Reads current game statistics and raises the StatsUpdated event
        // Includes error handling to prevent timer disruption
        // ==========MY NOTES==============
        // This runs every second to check for new values
        // It gets fresh stats and tells listeners about the changes
        // The try-catch makes sure timer keeps working even if something fails
        private void UpdateCallback(object? state)
        {
            try
            {
                if (!_isUpdating || _updateTimer == null)
                    return;

                var stats = GetStats(); // This now calls the derived class implementation

                StatsUpdated?.Invoke(this, new StatsUpdatedEventArgs(
                    stats.Percent, stats.PercentFloat, stats.Viewpoints, stats.Myan,
                    stats.Treasure, stats.Fragments, stats.Assassin, stats.Naval,
                    stats.Letters, stats.Manuscripts, stats.Music, stats.Forts,
                    stats.Taverns, stats.TotalChests));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in update timer callback: {ex.Message}");
            }
        }

        // ==========FORMAL COMMENT=========
        // Stops the automatic updates and releases timer resources
        // Safely handles cleanup of the timer to prevent memory leaks
        // ==========MY NOTES==============
        // Shuts down the auto-update system cleanly
        // Makes sure we don't leave timers running or resources locked
        // Called when disconnecting or closing the app
        public void StopUpdating()
        {
            if(!_isUpdating) return;

            _isUpdating = false;

            // stop and dispose timer
            if (_updateTimer != null)
            {
                _updateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                _updateTimer.Dispose();
                _updateTimer = null;
            }
        }

        // ==========FORMAL COMMENT=========
        // Windows API import for reading data from the memory of another process
        // Required for accessing the game's memory space
        // ==========MY NOTES==============
        // This is the Windows function that lets us peek into the game's memory
        // Without this, we couldn't read any stats
        #pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            nint lpBaseAddress,
            void* lpBuffer,
            nint nSize,
            out nint lpNumberOfBytesRead);
    }

   
}

