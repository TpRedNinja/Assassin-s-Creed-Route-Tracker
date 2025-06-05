using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Assassin_s_Creed_Route_Tracker
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
    public unsafe class GameStats
    {
        // Add architecture field
        private readonly bool _is64BitProcess;

        // Update constructor to accept architecture info
        public GameStats(IntPtr processHandle, IntPtr baseAddress, bool is64BitProcess)
        {
            this.processHandle = processHandle;
            this.baseAddress = baseAddress;
            this.collectiblesBaseAddress = (nint)baseAddress + 0x026BEAC0;
            _is64BitProcess = is64BitProcess;
            Debug.WriteLine($"GameStats initialized for {(is64BitProcess ? "64-bit" : "32-bit")} process");
        }

        // Your existing fields would go here
        private readonly IntPtr processHandle;
        private readonly IntPtr baseAddress;

        // Timer-related fields for automatic stat updates
        private System.Threading.CancellationTokenSource? _updateCancellationTokenSource;
        private bool _isUpdating = false;
        private readonly int _updateIntervalMs = 1000; // Default update interval of 1 second
        private System.Threading.Timer? _updateTimer; // Timer that controls the periodic stat updates

        // Event that fires whenever game statistics change
        // UI components can subscribe to this to get real-time updates
        public event EventHandler<StatsUpdatedEventArgs>? StatsUpdated;

        // ==========FORMAL COMMENT=========
        // Memory offset arrays and constants for accessing game statistics
        // Special cases (percent, percentFloat, forts) use unique memory paths
        // Most collectibles share a common base address and offset pattern with varying third offsets
        // ==========MY NOTES==============
        // These offsets are our map to find values in the game's memory
        // Most collectibles follow the same pattern (only third offset changes)
        // A few values like completion percentage and forts need special handling
        private readonly int[] percentPtrOffsets = [0x284];
        private readonly int[] percentFtPtrOffsets = [0x74];
        private readonly int[] fortsPtrOffsets = [0x7F0, 0xD68, 0xD70, 0x30];

        // Pre-calculated base address for most collectibles to avoid repeated calculations
        private readonly nint collectiblesBaseAddress;

        // Third offsets for collectibles totals counters
        private const int ViewpointsThirdOffset = -0x1B30;
        private const int MyanThirdOffset = -0x1B1C;
        private const int TreasureThirdOffset = -0xBB8;
        private const int FragmentsThirdOffset = -0x1B58;
        private const int AssassinThirdOffset = -0xDD4;
        private const int NavalThirdOffset = -0x19F0;
        private const int LettersThirdOffset = -0x04EC;
        private const int ManuscriptsThirdOffset = -0x334;
        private const int MusicThirdOffset = 0x424;

        // Offsets that are used for most collectibles
        private const int FirstOffset = 0x2D0;
        private const int SecondOffset = 0x8BC;
        private const int LastOffset = 0x18;

        //step offset for collectibles that are stored as individual flags
        private const int OffsetStep = 0x14;

        // The start and ending offsets for Chests
        // The first offset is for the first address that has one of the Chest address
        // The end offset is the offset for the last Chests address
        private const int ChestStartOffset = 0x67C;
        private const int ChestEndOffset = 0xA8C;

        // The start and ending offsets for Taverns
        // The first offset is for the first address that has one of the taversn address
        // The end offset is the offset for the last tavern address
        private const int TavernStartOffset = 0x319C;
        private const int TavernEndOffset = 0x3228;

        // ==========FORMAL COMMENT=========
        // Generic method to read values from game memory
        // Follows chains of pointers using the provided offsets
        // ==========MY NOTES==============
        // This is the low-level function that actually reads from memory
        // It follows the trail of addresses to find the specific value we want
        private unsafe T Read<T>(nint baseAddress, int[] offsets) where T : unmanaged
        {
            // Call the appropriate read method based on architecture
            return _is64BitProcess
                ? Read64<T>(baseAddress, offsets)
                : Read32<T>(baseAddress, offsets);
        }

        // Original 32-bit pointer handling (your current code)
        private unsafe T Read32<T>(nint baseAddress, int[] offsets) where T : unmanaged
        {
            nint address = baseAddress;
            Debug.WriteLine($"[32-bit] Reading memory at base address: {baseAddress:X}");

            int pointer_size = 4;
            foreach (int offset in offsets)
            {
                if (!ReadProcessMemory(processHandle, address, &address, pointer_size, out nint bytesReads) || bytesReads != pointer_size || address == IntPtr.Zero)
                {
                    Debug.WriteLine($"[32-bit] Failed to read memory address at offset {offset:X}");
                    return default;
                }
                address += offset;
                Debug.WriteLine($"[32-bit] Address after applying offset {offset:X}: {address:X}");
            }

            T value;
            int size = sizeof(T);
            if (!ReadProcessMemory(processHandle, address, &value, size, out nint bytesRead) || bytesRead != size)
            {
                Debug.WriteLine($"[32-bit] Failed to read value from memory at address {address:X}");
                return default;
            }

            Debug.WriteLine($"[32-bit] Read value from memory at address {address:X}: {value}");
            return value;
        }

        // New 64-bit pointer handling
        private unsafe T Read64<T>(nint baseAddress, int[] offsets) where T : unmanaged
        {
            nint address = baseAddress;
            Debug.WriteLine($"[64-bit] Reading memory at base address: {baseAddress:X}");

            int pointer_size = 8; // 64-bit pointers are 8 bytes
            foreach (int offset in offsets)
            {
                long pointer = 0;
                if (!ReadProcessMemory(processHandle, address, &pointer, pointer_size, out nint bytesReads) || bytesReads != pointer_size || pointer == 0)
                {
                    Debug.WriteLine($"[64-bit] Failed to read memory address at offset {offset:X}");
                    return default;
                }
                address = (nint)pointer + offset;
                Debug.WriteLine($"[64-bit] Address after applying offset {offset:X}: {address:X}");
            }

            T value;
            int size = sizeof(T);
            if (!ReadProcessMemory(processHandle, address, &value, size, out nint bytesRead) || bytesRead != size)
            {
                Debug.WriteLine($"[64-bit] Failed to read value from memory at address {address:X}");
                return default;
            }

            Debug.WriteLine($"[64-bit] Read value from memory at address {address:X}: {value}");
            return value;
        }

        // ==========FORMAL COMMENT=========
        // Helper method to read collectibles using shared memory pattern
        // Uses common base address and offset structure with specific third offset
        // ==========MY NOTES==============
        // Simplifies reading collectibles that follow the standard pattern
        // Makes the code cleaner by removing duplicated memory reading logic
        private int ReadCollectible(int thirdOffset)
        {
            return Read<int>(collectiblesBaseAddress, [FirstOffset, SecondOffset, thirdOffset, LastOffset]);
        }

        // ==========FORMAL COMMENT=========
        // Helper method to count collectibles that are stored as individual flags
        // Iterates through a range of third offsets and sums their values
        // ==========MY NOTES==============
        // Used for things like chests and taverns that have many individual locations
        // Each location has its own memory address with a predictable pattern
        private int CountCollectibles(int startOffset, int endOffset)
        {
            int count = 0;
            for (int thirdOffset = startOffset; thirdOffset <= endOffset; thirdOffset += OffsetStep)
            {
                count += ReadCollectible(thirdOffset);
            }
            return count;
        }

        // ==========FORMAL COMMENT=========
        // Retrieves all game statistics from memory in a single operation
        // Handles both special case stats and standard collectibles with appropriate methods
        // Returns a tuple containing all available game progress metrics
        // ==========MY NOTES==============
        // Reads all the different stats at once and returns them as one package
        // Uses the simplified helper for most collectibles and special handling for others
        // This is the main method that gets called when displaying or updating stats
        public (int Percent, float PercentFloat, int Viewpoints, int Myan, int Treasure, int Fragments, int Assassin, int Naval, int Letters, int Manuscripts, int Music, int Forts, int Taverns, int TotalChests) GetStats()
        {
            // reading collectibles not using simplified helper method
            int percent = Read<int>((nint)baseAddress + 0x49D9774, percentPtrOffsets);
            float percentfloat = Read<float>((nint)baseAddress + 0x049F1EE8, percentFtPtrOffsets);
            int forts = Read<int>((nint)baseAddress + 0x026BE51C, fortsPtrOffsets);

            // Read collectibles using the simplified helper method
            int viewpoints = ReadCollectible(ViewpointsThirdOffset);
            int myan = ReadCollectible(MyanThirdOffset);
            int treasure = ReadCollectible(TreasureThirdOffset);
            int fragments = ReadCollectible(FragmentsThirdOffset);
            int assassin = ReadCollectible(AssassinThirdOffset);
            int naval = ReadCollectible(NavalThirdOffset);
            int letters = ReadCollectible(LettersThirdOffset);
            int manuscripts = ReadCollectible(ManuscriptsThirdOffset);
            int music = ReadCollectible(MusicThirdOffset);

            // Count collectibles that are stored as multiple individual flags
            int taverns = CountCollectibles(TavernStartOffset, TavernEndOffset);
            int totalChests = CountCollectibles(ChestStartOffset, ChestEndOffset);

            return (percent, percentfloat, viewpoints, myan, treasure, fragments, assassin, naval, letters, manuscripts, music, forts, taverns, totalChests);
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
                // Check if we should still be updating
                if (!_isUpdating || _updateTimer == null)
                    return;

                // Get current stats
                (int Percent, float PercentFloat, int Viewpoints, int Myan, int Treasure, int Fragments, int Assassin, int Naval, int Letters, int Manuscripts, int Music, int Forts, int Taverns, int TotalChests) = GetStats();

                // Notify listeners (sync invoke to avoid thread issues)
                StatsUpdated?.Invoke(this, new StatsUpdatedEventArgs(
                    Percent,
                    PercentFloat,
                    Viewpoints,
                    Myan,
                    Treasure,
                    Fragments,
                    Assassin,
                    Naval,
                    Letters,
                    Manuscripts,
                    Music,
                    Forts,
                    Taverns,
                    TotalChests));
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
