using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Route_Tracker
{
    /// <summary>
    /// Manages route entries, their completion status, and UI interactions
    /// </summary>
    public class RouteManager
    {
        private readonly string routeFilePath;
        private readonly GameConnectionManager gameConnectionManager;
        private List<RouteEntry> routeEntries = new List<RouteEntry>();

        // last know folder path for Routes folder to avoid repeated searches
        private static string? lastFoundRoutesFolder;

        // Add field to track last known percentage for detecting save file changes
        private float lastKnownPercentage = 0f;
        private bool isFirstUpdate = true;

        public RouteManager(string routeFilePath, GameConnectionManager gameConnectionManager)
        {
            this.routeFilePath = routeFilePath;
            this.gameConnectionManager = gameConnectionManager;
        }

        /// <summary>
        /// Loads route entries from the TSV file
        /// </summary>
        public List<RouteEntry> LoadEntries()
        {
            RouteLoader routeLoader = new RouteLoader();
            if (File.Exists(routeFilePath))
            {
                string filename = Path.GetFileName(routeFilePath);
                routeEntries = routeLoader.LoadRoute(filename);
            }
            return routeEntries;
        }

        /// <summary>
        /// Loads route data into the grid view with diagnostic messages
        /// </summary>
        public void LoadRouteDataIntoGrid(DataGridView routeGrid, string gameName)
        {
            routeGrid.Rows.Clear();
            routeGrid.Rows.Add("Searching everywhere for route files...", "");

            try
            {
                // First try to find any Routes folder
                string? routesFolder = FindRoutesFolderAnywhere();
                if (routesFolder == null)
                {
                    routeGrid.Rows.Add("ERROR: Could not find any Routes folder anywhere!", "");
                    routeGrid.Rows.Add("Please create a 'Routes' folder somewhere on your system", "");
                    return;
                }

                routeGrid.Rows.Add($"Found Routes folder at: {routesFolder}", "");

                // Find any TSV file that might be a route file
                string[] tsvFiles = Directory.GetFiles(routesFolder, "*.tsv");
                if (tsvFiles.Length == 0)
                {
                    routeGrid.Rows.Add("ERROR: No TSV files found in Routes folder!", "");
                    return;
                }

                // Pick the first file that might be relevant
                string? routeFile = null;
                foreach (var file in tsvFiles)
                {
                    string filename = Path.GetFileName(file).ToLower();
                    // Try to find a file that seems related to AC4 routes
                    if (filename.Contains("ac4") || filename.Contains("assassin") ||
                        filename.Contains("route") || filename.Contains("main"))
                    {
                        routeFile = file;
                        break;
                    }
                }

                // If no specific match, just take the first TSV file
                routeFile ??= tsvFiles[0];

                routeGrid.Rows.Add($"Using route file: {routeFile}", "");

                // Load the route entries
                List<RouteEntry> entries = LoadRouteFromPath(routeFile);
                if (entries.Count == 0)
                {
                    routeGrid.Rows.Add("ERROR: No valid entries found in the route file", "");
                    return;
                }

                // Clear diagnostics and show actual entries
                routeGrid.Rows.Clear();
                foreach (var entry in entries)
                {
                    string completionMark = entry.IsCompleted ? "X" : "";
                    int rowIndex = routeGrid.Rows.Add(entry.DisplayText, completionMark);
                    routeGrid.Rows[rowIndex].Tag = entry;
                }

                // Store the entries for future reference
                routeEntries = entries;

                // Sort and scroll
                SortAndScrollToFirstIncomplete(routeGrid);
            }
            catch (Exception ex)
            {
                routeGrid.Rows.Add($"Error: {ex.Message}", "");
                routeGrid.Rows.Add($"Stack trace: {ex.StackTrace}", "");
            }
        }

        private string? FindRoutesFolderAnywhere()
        {
            // First check the cached location from previous searches
            if (lastFoundRoutesFolder != null && Directory.Exists(lastFoundRoutesFolder))
            {
                return lastFoundRoutesFolder; // Use cached path if it still exists
            }

            // Places to check in order of likelihood
            List<string> possibleLocations = new List<string>
    {
        // App folder and nearby
        AppDomain.CurrentDomain.BaseDirectory,
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."),
        
        // User folders
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
        
        // Project folders (when running from IDE)
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..")
    };

            // First check direct Routes folders in common locations
            foreach (var location in possibleLocations)
            {
                try
                {
                    string potentialPath = Path.Combine(location, "Routes");
                    if (Directory.Exists(potentialPath))
                        return potentialPath;
                }
                catch { /* Skip any locations we can't access */ }
            }

            // Next, try to find a Routes folder in any children of common locations
            foreach (var location in possibleLocations)
            {
                try
                {
                    if (!Directory.Exists(location))
                        continue;

                    foreach (var dir in Directory.GetDirectories(location))
                    {
                        try
                        {
                            // Check for Routes folder in this directory
                            string potentialPath = Path.Combine(dir, "Routes");
                            if (Directory.Exists(potentialPath))
                            {
                                lastFoundRoutesFolder = potentialPath;
                                return potentialPath;
                            }

                            // Also check children directories one level deeper
                            foreach (var subdir in Directory.GetDirectories(dir))
                            {
                                try
                                {
                                    string subPath = Path.Combine(subdir, "Routes");
                                    if (Directory.Exists(potentialPath))
                                    {
                                        lastFoundRoutesFolder = potentialPath;
                                        return potentialPath;
                                    }
                                }
                                catch { /* Skip any we can't access */ }
                            }
                        }
                        catch { /* Skip any we can't access */ }
                    }
                }
                catch { /* Skip any locations we can't access */ }
            }

            // Last resort: check if any folder named "Routes" exists in user profile recursively
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string? foundPath = FindRoutesFolderRecursive(userProfile, 3);
                if (foundPath != null)
                {
                    lastFoundRoutesFolder = foundPath;
                }
                return foundPath;
            }
            catch { /* Skip if we can't access */ }

            return null; // No Routes folder found anywhere
        }

        private string? FindRoutesFolderRecursive(string startPath, int maxDepth)
        {
            if (maxDepth <= 0) return null;

            try
            {
                // Check this directory itself
                string potentialPath = Path.Combine(startPath, "Routes");
                if (Directory.Exists(potentialPath))
                {
                    lastFoundRoutesFolder = potentialPath; // Cache it here too
                    return potentialPath;
                }

                // Check subdirectories
                foreach (var dir in Directory.GetDirectories(startPath))
                {
                    try
                    {
                        // Skip system folders to speed things up and avoid permission issues
                        if (dir.EndsWith("Windows") || dir.EndsWith("Program Files") ||
                            dir.EndsWith("Program Files (x86)") || dir.EndsWith("$Recycle.Bin"))
                            continue;

                        string result = FindRoutesFolderRecursive(dir, maxDepth - 1);
                        if (result != null)
                            return result;
                    }
                    catch { /* Skip any we can't access */ }
                }
            }
            catch { /* Skip if we can't access this directory */ }

            return null;
        }

        private List<RouteEntry> LoadRouteFromPath(string fullPath)
        {
            List<RouteEntry> entries = new List<RouteEntry>();

            foreach (string line in File.ReadAllLines(fullPath))
            {
                // Skip blank lines or lines with insufficient data
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split('\t');
                if (parts.Length >= 3)
                {
                    string displayText = parts[0].Trim();

                    // Skip entries with blank display text
                    if (string.IsNullOrWhiteSpace(displayText))
                        continue;

                    string conditionType = parts[1].Trim();

                    if (int.TryParse(parts[2].Trim(), out int conditionValue))
                    {
                        RouteEntry entry = new RouteEntry(displayText, conditionType, conditionValue);
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        private bool wasResetAfter100Percent = false; // Track if we've already reset after dropping from 100%

        /// <summary>
        /// Updates the completion status of route entries based on current game stats
        /// </summary>
        public bool UpdateCompletionStatus(DataGridView routeGrid, StatsUpdatedEventArgs stats)
        {
            bool anyChanges = false;
            bool is100Percent = (stats.Percent == 100 || Math.Abs(stats.PercentFloat - 100.0f) < 0.01f);
            bool was100Percent = !isFirstUpdate && (Math.Abs(lastKnownPercentage - 100.0f) < 0.01f);

            // Reset needed ONLY when transitioning from 100% to not 100%
            // AND we haven't already reset since that transition
            bool needsReset = was100Percent && !is100Percent && !wasResetAfter100Percent;

            if (needsReset)
            {
                Debug.WriteLine("Game dropped from 100% - resetting all entries to not completed");

                // Reset all entries to false
                foreach (DataGridViewRow row in routeGrid.Rows)
                {
                    if (row.Tag is RouteEntry entry)
                    {
                        if (entry.IsCompleted)
                        {
                            entry.IsCompleted = false;
                            row.Cells[1].Value = "";
                            anyChanges = true;
                        }
                    }
                }

                // Mark that we've done the reset after 100%, so we don't do it again
                wasResetAfter100Percent = true;
            }

            // If we're at 100% now, clear the reset flag so we'll reset again if needed later
            if (is100Percent)
            {
                wasResetAfter100Percent = false;
            }

            // Log stats for debugging
            string statsInfo = $"Stats: Percent={stats.Percent}%, PercentFloat={stats.PercentFloat}%, " +
                 $"Viewpoints={stats.Viewpoints}, Myan={stats.Myan}, " +
                 $"Treasure={stats.Treasure}, Fragments={stats.Fragments}, " +
                 $"Assassin={stats.Assassin}, Naval={stats.Naval}";
            Debug.WriteLine(statsInfo);

            // Now check completion for all entries based on current stats
            foreach (DataGridViewRow row in routeGrid.Rows)
            {
                if (row.Tag is RouteEntry entry)
                {
                    // At 100%, everything should be completed
                    // Otherwise use normal completion checking
                    bool shouldBeCompleted = is100Percent || CheckCompletion(entry, stats);

                    // Only update if completion status has changed
                    if (shouldBeCompleted != entry.IsCompleted)
                    {
                        entry.IsCompleted = shouldBeCompleted;
                        row.Cells[1].Value = shouldBeCompleted ? "X" : "";
                        anyChanges = true;
                    }
                }
            }

            // Update our saved percentage for next comparison
            lastKnownPercentage = stats.PercentFloat;
            isFirstUpdate = false;

            // If changes were made, sort and scroll
            if (anyChanges)
            {
                SortAndScrollToFirstIncomplete(routeGrid);
            }

            return anyChanges;
        }

        /// <summary>
        /// Checks if a route entry is completed based on the current game stats
        /// </summary>
        private bool CheckCompletion(RouteEntry entry, StatsUpdatedEventArgs stats)
        {
            // If game is 100% complete, everything should be marked as completed
            if (stats.Percent == 100 || Math.Abs(stats.PercentFloat - 100.0f) < 0.01f)
            {
                return true;
            }

            if (string.IsNullOrEmpty(entry.Type))
                return false; // Default to false for entries with no type

            // Normalize the type: lowercase and remove spaces
            string normalizedType = entry.Type.ToLowerInvariant().Replace(" ", "");

            // Debug to see what types are being processed
            Debug.WriteLine($"Checking entry: {entry.DisplayText}, Type: {normalizedType}, Condition: {entry.Condition}");

            // Get the special activity counts from the game stats
            var specialCounts = gameConnectionManager?.GameStats is AC4GameStats ac4GameStats
                ? ac4GameStats.GetSpecialActivityCounts()
                : (0, 0, 0);

            // Check special percentage-based activities - access tuple elements by position
            if (normalizedType.Contains("story") || normalizedType.Contains("mainstory"))
            {
                return specialCounts.Item1 >= entry.Condition;  // Item1 = StoryMissions
            }
            else if (normalizedType.Contains("templar"))
            {
                return specialCounts.Item2 >= entry.Condition;  // Item2 = TemplarHunts
            }
            else if (normalizedType.Contains("legendary"))
            {
                return specialCounts.Item3 >= entry.Condition;  // Item3 = LegendaryShips
            }

            return normalizedType switch
            {
                // Viewpoints
                "viewpoint" or "viewpoints" => stats.Viewpoints >= entry.Condition,

                // Mayan stones
                "mayan" or "myan" or "myanstones" or "mayanstones" => stats.Myan >= entry.Condition,

                // Treasure
                "treasure" or "treasures" or "burired treasure" or "buried treasure" or "buriredtreasure" or "buried treasure" => stats.Treasure >= entry.Condition,

                // Animus fragments
                "fragment" or "fragments" or "animusfragment" or "animusfragments" => stats.Fragments >= entry.Condition,

                // Assassin contracts
                "assassin" or "assassincontract" or "assassincontracts" => stats.Assassin >= entry.Condition,

                // Naval contracts
                "naval" or "navalcontract" or "navalcontracts" => stats.Naval >= entry.Condition,

                // Message bottles
                "letter" or "letters" or "letterbottle" or "letterbottles" => stats.Letters >= entry.Condition,

                // Manuscripts
                "manuscript" or "manuscripts" => stats.Manuscripts >= entry.Condition,

                // Music/shanties
                "music" or "musicsheet" or "musicsheets" or "shanty" or "shanties" => stats.Music >= entry.Condition,

                // Forts
                "fort" or "forts" => stats.Forts >= entry.Condition,

                // Taverns
                "tavern" or "taverns" => stats.Taverns >= entry.Condition,

                // Chests
                "chest" or "chests" => stats.TotalChests >= entry.Condition,

                // Manual completion items - should return false by default
                "upgrades" => false,

                // Default: unrecognized types - default to false
                _ => false
            };
        }

        /// <summary>
        /// Sorts route entries to show completed items at the top and scrolls to first non-completed item
        /// </summary>
        public void SortAndScrollToFirstIncomplete(DataGridView routeGrid)
        {
            if (routeGrid == null || routeGrid.Rows.Count == 0)
                return;

            // Get all entries from the grid
            List<RouteEntry> entries = new List<RouteEntry>();
            Dictionary<RouteEntry, int> originalIndices = new Dictionary<RouteEntry, int>();

            for (int i = 0; i < routeGrid.Rows.Count; i++)
            {
                if (routeGrid.Rows[i].Tag is RouteEntry entry)
                {
                    entries.Add(entry);
                    originalIndices[entry] = i; // Track original position
                }
            }

            // Sort entries: completed first, then original order within groups
            var sortedEntries = entries
                .OrderByDescending(e => e.IsCompleted)
                .ThenBy(e => originalIndices[e])
                .ToList();

            // Clear and re-add rows in sorted order
            routeGrid.Rows.Clear();
            int firstIncompleteIndex = -1;

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var entry = sortedEntries[i];
                string completionMark = entry.IsCompleted ? "X" : "";
                int rowIndex = routeGrid.Rows.Add(entry.DisplayText, completionMark);
                routeGrid.Rows[rowIndex].Tag = entry;

                // Track first non-completed entry
                if (firstIncompleteIndex == -1 && !entry.IsCompleted)
                    firstIncompleteIndex = rowIndex;
            }

            // Scroll to first non-completed entry
            if (firstIncompleteIndex >= 0)
            {
                routeGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, firstIncompleteIndex - 2);
                routeGrid.ClearSelection();
                routeGrid.Rows[firstIncompleteIndex].Selected = true;
            }
            else if (routeGrid.Rows.Count > 0)
            {
                routeGrid.FirstDisplayedScrollingRowIndex = 0;
            }
        }
    }
}
