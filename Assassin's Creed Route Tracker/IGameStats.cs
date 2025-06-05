using System;
using System.Collections.Generic;

namespace Assassin_s_Creed_Route_Tracker
{
    /// <summary>
    /// Interface defining the contract for game statistics implementations
    /// Provides a common API for different game-specific stat implementations
    /// </summary>
    public interface IGameStats
    {
        event EventHandler<StatsUpdatedEventArgs>? StatsUpdated;
        void StartUpdating();
        void StopUpdating();
        Dictionary<string, object> GetStatsAsDictionary();

        (int Percent, float PercentFloat, int Viewpoints, int Myan, int Treasure,
        int Fragments, int Assassin, int Naval, int Letters, int Manuscripts,
        int Music, int Forts, int Taverns, int TotalChests) GetStats();
    }
}