using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Assassin_s_Creed_Route_Tracker.Properties;

namespace Assassin_s_Creed_Route_Tracker
{
    // ==========FORMAL COMMENT=========
    // Manages application settings and game directories
    // Handles loading, saving, and providing access to configuration values
    // ==========MY NOTES==============
    // This centralizes all settings-related code in one place
    // Makes it easier to access and update settings from anywhere
    public class SettingsManager
    {
        private bool isLoadingSettings = false;

        // ==========FORMAL COMMENT=========
        // Loads user settings into UI controls
        // Uses a flag to prevent change events during loading
        // ==========MY NOTES==============
        // Fills UI controls with saved settings when app starts
        [SupportedOSPlatform("windows6.1")]
        public void LoadSettings(TextBox gameDirectoryTextBox, CheckBox autoStartCheckBox)
        {
            isLoadingSettings = true;
            gameDirectoryTextBox.Text = Settings.Default.GameDirectory;
            autoStartCheckBox.Checked = Settings.Default.AutoStart;
            isLoadingSettings = false;
        }

        // ==========FORMAL COMMENT=========
        // Saves settings from UI controls to configuration
        // ==========MY NOTES==============
        // Stores current settings so they're remembered next time
        [SupportedOSPlatform("windows6.1")]
        public void SaveSettings(string gameDirectory, bool autoStart)
        {
            Settings.Default.GameDirectory = gameDirectory;
            Settings.Default.AutoStart = autoStart;
            Settings.Default.Save();
        }

        // ==========FORMAL COMMENT=========
        // Saves game-specific directory path
        // Selects the appropriate setting based on game name
        // ==========MY NOTES==============
        // Stores the game executable location for a specific game
        [SupportedOSPlatform("windows6.1")]
        public void SaveDirectory(string selectedGame, string directory)
        {
            if (selectedGame == "Assassin's Creed 4")
            {
                Settings.Default.AC4Directory = directory;
            }
            else if (selectedGame == "Assassin's Creed Syndicate")
            {
                Settings.Default.ACSDirectory = directory;
            }
            Settings.Default.Save();
        }

        // ==========FORMAL COMMENT=========
        // Retrieves the directory for the specified game
        // ==========MY NOTES==============
        // Gets the saved path for a specific game
        [SupportedOSPlatform("windows6.1")]
        public string GetGameDirectory(string game)
        {
            if (game == "Assassin's Creed 4")
                return Settings.Default.AC4Directory;
            else if (game == "Assassin's Creed Syndicate")
                return Settings.Default.ACSDirectory;
            return string.Empty;
        }

        // ==========FORMAL COMMENT=========
        // Provides access to loading state for event handlers
        // ==========MY NOTES==============
        // Helps prevent events from triggering during initial load
        public bool IsLoadingSettings => isLoadingSettings;
    }
}
