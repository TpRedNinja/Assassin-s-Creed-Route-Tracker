using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Route_Tracker
{
    public class RouteEntry
    {
        // Basic properties from TSV file
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Condition { get; set; }
        public string Coordinates { get; set; } = string.Empty;
        public string LocationCondition { get; set; } = string.Empty;
        public string? ConditionType { get; set; } // e.g., "Viewpoints", "Myan", etc.
        public int? ConditionValue { get; set; } // The value needed for completion

        // Runtime state
        public bool IsCompleted { get; set; } = false;

        public RouteEntry(string name, string type = "", int condition = 0)
        {
            Name = name;
            Type = type;
            Condition = condition;
            IsCompleted = false;
        }

        // Returns Name with coordinates if they exist
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(Coordinates))
                    return $"{Name} [{Coordinates}]";
                else
                    return Name;
            }
        }
    }
}