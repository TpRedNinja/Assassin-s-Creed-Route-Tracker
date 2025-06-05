using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Route_Tracker
{
    public class RouteLoader
    {
        /// <summary>
        /// Loads a route from a TSV file for the specified game
        /// </summary>
        /// <param name="filename">The name of the route file without path</param>
        /// <returns>A list of RouteEntry objects representing the route</returns>
        public List<RouteEntry> LoadRoute(string filename)
        {
            List<RouteEntry> entries = new List<RouteEntry>();
            string routePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Routes", filename);

            if (File.Exists(routePath))
            {
                foreach (string line in File.ReadAllLines(routePath))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length >= 3) // Assuming at least display text, type, condition value
                    {
                        string displayText = parts[0].Trim();
                        string collectibleType = parts[1].Trim().ToLowerInvariant(); // Convert to lowercase for consistency

                        if (int.TryParse(parts[2].Trim(), out int conditionValue))
                        {
                            // Create RouteEntry with proper type and condition
                            RouteEntry entry = new RouteEntry(displayText, collectibleType, conditionValue);
                            entries.Add(entry);

                            // Debug output to help diagnose issues
                            Console.WriteLine($"Loaded: {displayText}, Type: {collectibleType}, Condition: {conditionValue}");
                        }
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Gets a list of available route files
        /// </summary>
        /// <returns>Array of route file names</returns>
        public string[] GetAvailableRoutes()
        {
            string routeDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Routes");

            if (!Directory.Exists(routeDirectory))
                return Array.Empty<string>();

            try
            {
                return Directory.GetFiles(routeDirectory, "*.tsv")
                                .Select(Path.GetFileName)
                                .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}