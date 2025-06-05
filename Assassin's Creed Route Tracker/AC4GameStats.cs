using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Route_Tracker
{
    public unsafe class AC4GameStats : GameStatsBase
    {
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

        // Add fields to track percentage-based activities
        private float lastPercentageValue = 0f;
        private int completedStoryMissions = 0;
        private int completedTemplarHunts = 0;
        private int defeatedLegendaryShips = 0;

        // Exact percentage values to check
        private const float LEGENDARY_SHIP_PERCENT = 0.18750f;
        private const float TEMPLAR_HUNT_MIN = 0.38579f;
        private const float TEMPLAR_HUNT_MAX = 0.38582f;
        private const float STORY_MISSION_MIN = 0.66666f;
        private const float STORY_MISSION_MAX = 1.66668f;
        private const float DETECTION_THRESHOLD = 0.00001f;

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
        public AC4GameStats(IntPtr processHandle, IntPtr baseAddress)
        : base(processHandle, baseAddress)
        {
            this.collectiblesBaseAddress = (nint)baseAddress + 0x026BEAC0;
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

        // Implementation of GetStats for AC4
        // Modified GetStats implementation with percentage change detection
        public override (int Percent, float PercentFloat, int Viewpoints, int Myan, int Treasure,
            int Fragments, int Assassin, int Naval, int Letters, int Manuscripts, int Music,
            int Forts, int Taverns, int TotalChests) GetStats()
        {
            // Reading collectibles using existing methods
            int percent = Read<int>((nint)baseAddress + 0x49D9774, percentPtrOffsets);
            float percentFloat = Read<float>((nint)baseAddress + 0x049F1EE8, percentFtPtrOffsets);
            int forts = Read<int>((nint)baseAddress + 0x026C0A28, fortsPtrOffsets);

            // Read all other collectibles
            int viewpoints = ReadCollectible(ViewpointsThirdOffset);
            int myan = ReadCollectible(MyanThirdOffset);
            int treasure = ReadCollectible(TreasureThirdOffset);
            int fragments = ReadCollectible(FragmentsThirdOffset);
            int assassin = ReadCollectible(AssassinThirdOffset);
            int naval = ReadCollectible(NavalThirdOffset);
            int letters = ReadCollectible(LettersThirdOffset);
            int manuscripts = ReadCollectible(ManuscriptsThirdOffset);
            int music = ReadCollectible(MusicThirdOffset);
            int taverns = CountCollectibles(TavernStartOffset, TavernEndOffset);
            int totalChests = CountCollectibles(ChestStartOffset, ChestEndOffset);

            // Detect percentage-based activities
            DetectSpecialActivities(percentFloat);

            // Return all the stats (including the basic ones that we got from memory)
            return (percent, percentFloat, viewpoints, myan, treasure, fragments, assassin, naval,
                letters, manuscripts, music, forts, taverns, totalChests);
        }

        // New method to detect special activities based on percentage changes
        private void DetectSpecialActivities(float currentPercentage)
        {
            // If this is the first read, just store the value
            if (lastPercentageValue == 0f)
            {
                lastPercentageValue = currentPercentage;
                return;
            }

            // Calculate the change since last reading
            float percentageDelta = currentPercentage - lastPercentageValue;

            // Only process positive changes (completed activities)
            if (percentageDelta > 0)
            {
                // Log the percentage change for debugging
                Debug.WriteLine($"Percentage delta: {percentageDelta:F10}");

                // Detect legendary ships (exact match with small tolerance)
                if (Math.Abs(percentageDelta - LEGENDARY_SHIP_PERCENT) < DETECTION_THRESHOLD)
                {
                    defeatedLegendaryShips++;
                    Debug.WriteLine($"Legendary ship defeated! Total: {defeatedLegendaryShips}");
                }
                // Detect Templar hunts (within range)
                else if (percentageDelta >= TEMPLAR_HUNT_MIN && percentageDelta <= TEMPLAR_HUNT_MAX)
                {
                    completedTemplarHunts++;
                    Debug.WriteLine($"Templar hunt completed! Total: {completedTemplarHunts}");
                }
                // Detect story missions (within broader range)
                else if (percentageDelta >= STORY_MISSION_MIN && percentageDelta <= STORY_MISSION_MAX)
                {
                    completedStoryMissions++;
                    Debug.WriteLine($"Story mission completed! Total: {completedStoryMissions}");
                }
                // For significant changes that don't match known patterns
                else if (percentageDelta > 0.1f)
                {
                    Debug.WriteLine($"Unrecognized percentage change: {percentageDelta:F10}");
                }
            }

            // Update the last value for the next comparison
            lastPercentageValue = currentPercentage;
        }

        // Method to get the special activity counts
        public (int StoryMissions, int TemplarHunts, int LegendaryShips) GetSpecialActivityCounts()
        {
            return (completedStoryMissions, completedTemplarHunts, defeatedLegendaryShips);
        }
    }
}
