using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assassin_s_Creed_Route_Tracker.Properties;

namespace Assassin_s_Creed_Route_Tracker
{
    // ==========FORMAL COMMENT=========
    // Main form class that provides the user interface for the Assassin's Creed Route Tracker
    // Handles connections to game processes, memory reading, and displaying game statistics
    // ==========MY NOTES==============
    // This is the main window of the app - it does everything from connecting to the game
    // to showing the stats and managing settings
    public partial class MainForm : Form
    {
        // ==========FORMAL COMMENT=========
        // Fields for process interaction and game memory access
        // ==========MY NOTES==============
        // These variables help us connect to the game and read its memory
        private string currentProcess = string.Empty; // Initialize with empty string
        private RouteManager? routeManager; // Mark as nullable since it's set after connection
        private readonly GameConnectionManager gameConnectionManager;
        private readonly SettingsManager settingsManager;

        private TabControl tabControl = null!; // Use null-forgiving operator since initialized in InitializeCustomComponents
        private TabPage statsTabPage = null!; // Same approach
        private TabPage routeTabPage = null!; // Same approach

        private TextBox gameDirectoryTextBox = null!; // Same approach
        private CheckBox autoStartCheckBox = null!; // Same approach

        // ==========FORMAL COMMENT=========
        // Constructor - initializes the form and loads user settings
        // ==========MY NOTES==============
        // This runs when the app starts - sets everything up
        [SupportedOSPlatform("windows6.1")]
        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();

            // Initialize managers
            gameConnectionManager = new GameConnectionManager();
            gameConnectionManager.StatsUpdated += GameStats_StatsUpdated;

            settingsManager = new SettingsManager();  // Add this line

            LoadSettings();
            this.FormClosing += MainForm_FormClosing;
        }

        // ==========FORMAL COMMENT=========
        // Creates all custom UI components for the application interface
        // Sets up the menu, tabs, buttons, and other controls
        // ==========MY NOTES==============
        // This builds all the buttons, tabs, and other stuff you see in the app
        [SupportedOSPlatform("windows6.1")]
        private void InitializeCustomComponents()
        {
            this.Text = "Assassin's Creed Route Tracker";
            this.BackColor = System.Drawing.Color.Black;
            this.ForeColor = System.Drawing.Color.White;

            // Create and configure the MenuStrip
            MenuStrip menuStrip = new()
            {
                Dock = DockStyle.Top, // Dock the MenuStrip at the top of the form
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.White
            };

            // Create and configure the Settings menu item
            ToolStripMenuItem settingsMenuItem = new("Settings");

            // Create and configure the Auto-Start Game menu item
            ToolStripMenuItem autoStartMenuItem = new("Auto-Start Game")
            {
                CheckOnClick = true
            };
            autoStartMenuItem.CheckedChanged += AutoStartMenuItem_CheckedChanged;

            // Create and configure the Game Directory menu item
            ToolStripMenuItem gameDirectoryMenuItem = new("Game Directory");
            gameDirectoryMenuItem.Click += GameDirectoryMenuItem_Click;

            // Add the Auto-Start Game and Game Directory menu items to the Settings menu item
            settingsMenuItem.DropDownItems.Add(autoStartMenuItem);
            settingsMenuItem.DropDownItems.Add(gameDirectoryMenuItem);

            // Add the Settings menu item to the MenuStrip
            menuStrip.Items.Add(settingsMenuItem);

            // Create and configure the Stats tab button
            ToolStripButton statsTabButton = new("Stats");
            statsTabButton.Click += (sender, e) => tabControl.SelectedTab = statsTabPage;
            menuStrip.Items.Add(statsTabButton);

            // Create and configure the Route tab button
            ToolStripButton routeTabButton = new("Route");
            routeTabButton.Click += (sender, e) => tabControl.SelectedTab = routeTabPage;
            menuStrip.Items.Add(routeTabButton);

            // Create and configure the connection label
            ToolStripLabel connectionLabel = new()
            {
                Text = "Not connected"
            };
            menuStrip.Items.Add(connectionLabel);

            // Create and configure the game dropdown
            ToolStripComboBox gameDropdown = new();
            gameDropdown.Items.AddRange(["", "Assassin's Creed 4", "Assassin's Creed Syndicate"]);
            gameDropdown.SelectedIndex = 0;
            menuStrip.Items.Add(gameDropdown);

            // Create and configure the connect button
            ToolStripButton connectButton = new("Connect to Game");
            connectButton.Click += ConnectButton_Click;
            menuStrip.Items.Add(connectButton);

            // Set the MenuStrip as the main menu strip of the form
            this.MainMenuStrip = menuStrip;

            // Add the MenuStrip to the form's controls
            this.Controls.Add(menuStrip);

            // Create and configure the TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Create and configure the Stats TabPage
            statsTabPage = new TabPage("Stats")
            {
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.White
            };

            Button percentageButton = new()
            {
                Text = "Stats",
                Location = new System.Drawing.Point(50, 10)
            };
            percentageButton.Click += PercentageButton_Click;
            statsTabPage.Controls.Add(percentageButton);

            Label percentageLabel = new()
            {
                Name = "percentageLabel",
                Text = "",
                Location = new System.Drawing.Point(50, 50),
                AutoSize = true
            };
            percentageLabel.Font = new Font(percentageLabel.Font.FontFamily, 14); // Set default font size to 14
            statsTabPage.Controls.Add(percentageLabel);

            // Create and configure the Route TabPage
            routeTabPage = new TabPage("Route")
            {
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.White
            };

            // Add the TabPages to the TabControl
            tabControl.TabPages.Add(statsTabPage);
            tabControl.TabPages.Add(routeTabPage);

            // Add the TabControl to the form's controls
            this.Controls.Add(tabControl);

            // Initialize gameDirectoryTextBox and autoStartCheckBox
            gameDirectoryTextBox = new TextBox
            {
                Location = new System.Drawing.Point((this.ClientSize.Width - 800) / 2, 100),
                Width = 600,
                ReadOnly = true,
                Visible = false
            };
            statsTabPage.Controls.Add(gameDirectoryTextBox);

            autoStartCheckBox = new CheckBox
            {
                Text = "Auto-Start Game",
                Location = new System.Drawing.Point((this.ClientSize.Width - 800) / 2, 130)
            };
            autoStartCheckBox.CheckedChanged += AutoStartCheckBox_CheckedChanged;
            autoStartCheckBox.Visible = false;
            statsTabPage.Controls.Add(autoStartCheckBox);
        }

        // ==========FORMAL COMMENT=========
        // Loads user settings from application configuration
        // Retrieves saved game directory and auto-start preference
        // ==========MY NOTES==============
        // Gets the saved settings when the app starts up
        // Uses a flag to prevent triggering events while loading
        [SupportedOSPlatform("windows6.1")]
        private void LoadSettings()
        {
            settingsManager.LoadSettings(gameDirectoryTextBox, autoStartCheckBox);
        }

        // ==========FORMAL COMMENT=========
        // Saves current application settings to the configuration file
        // Persists game directory and auto-start preferences for future sessions
        // ==========MY NOTES==============
        // Writes all settings to disk so they're remembered next time
        // Simple wrapper around the Settings.Save() functionality
        [SupportedOSPlatform("windows6.1")]
        private void SaveSettings()
        {
            settingsManager.SaveSettings(gameDirectoryTextBox.Text, autoStartCheckBox.Checked);
        }

        // ==========FORMAL COMMENT=========
        // Form closing event handler that ensures proper resource cleanup
        // Triggers cleanup operations before the form is destroyed
        // ==========MY NOTES==============
        // Runs when you close the application
        // Makes sure we clean up everything properly before exiting
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Clean up any resources before closing
            CleanupGameStats();
        }

        // ==========FORMAL COMMENT=========
        // Performs cleanup operations for GameStats resources
        // Unsubscribes from events and stops background updates to prevent memory leaks
        // ==========MY NOTES==============
        // This stops the automatic stat updates when we're done
        // Properly disconnects everything to avoid crashes and memory leaks
        // Important housekeeping to keep things tidy
        private void CleanupGameStats()
        {
            gameConnectionManager?.CleanupGameStats();
        }

        // ==========FORMAL COMMENT=========
        // Event handler for Connect button clicks
        // Handles UI interaction and delegates game connection to GameConnectionManager
        // ==========MY NOTES==============
        // This runs when you click "Connect to Game"
        // Manages the UI and uses GameConnectionManager for the actual connection work
        private async void ConnectButton_Click(object? sender, EventArgs e)
        {
            ToolStripComboBox? gameDropdown = this.MainMenuStrip?.Items.OfType<ToolStripComboBox>().FirstOrDefault();
            ToolStripLabel? connectionLabel = this.MainMenuStrip?.Items.OfType<ToolStripLabel>().FirstOrDefault();

            if (gameDropdown == null || connectionLabel == null)
            {
                MessageBox.Show("Required controls are missing.");
                return;
            }

            string selectedGame = gameDropdown.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(selectedGame))
            {
                connectionLabel.Text = "Please select a game.";
                return;
            }

            string gameDirectory = settingsManager.GetGameDirectory(selectedGame);

            if (string.IsNullOrEmpty(gameDirectory))
            {
                connectionLabel.Text = "Game directory not set.";
                return;
            }

            gameDirectoryTextBox.Text = gameDirectory;

            // Remember process name for later reference
            currentProcess = selectedGame == "Assassin's Creed 4" ? "AC4BFSP.exe" : "ACS.exe";

            // Use GameConnectionManager to handle the connection
            bool connected = await gameConnectionManager.ConnectToGameAsync(selectedGame, autoStartCheckBox.Checked);

            if (connected)
            {
                connectionLabel.Text = $"Connected to {selectedGame}";
                routeManager = new RouteManager("path_to_route_file.txt"); // Update with actual path
            }
            else
            {
                connectionLabel.Text = "Error: Cannot connect to process. Make sure the game is running.";
            }
        }

        // ==========FORMAL COMMENT=========
        // Event handler for GameStats statistics update events
        // Receives updated game metrics and refreshes the UI with the latest values
        // Uses thread-safe Invoke to update UI controls from the background thread
        // ==========MY NOTES==============
        // This catches the stats when they update automatically
        // Updates the UI safely across threads to show the new numbers
        // The real workhorse that keeps the display current without clicking buttons
        private void GameStats_StatsUpdated(object? sender, StatsUpdatedEventArgs e)
        {
            // we need to Invoke since this event comes from another thing
            if (statsTabPage.Controls["percentageLabel"] is Label percentageLabel)
            {
                this.Invoke(() => {
                    percentageLabel.Text = $"Completion Percentage: {e.Percent}%\n" +
                        $"Completion Percentage Exact: {Math.Round(e.PercentFloat, 2)}%\n" +
                        $"Viewpoints Completed: {e.Viewpoints}\n" +
                        $"Myan Stones Collected: {e.Myan}\n" +
                        $"Buried Treasure Collected: {e.Treasure}\n" +
                        $"AnimusFragments Collected: {e.Fragments}\n" +
                        $"AssassinContracts Completed: {e.Assassin}\n" +
                        $"NavalContracts Completed: {e.Naval}\n" +
                        $"LetterBottles Collected: {e.Letters}\n" +
                        $"Manuscripts Collected: {e.Manuscripts}\n" +
                        $"Music Sheets Collected: {e.Music}\n" +
                        $"Forts Captured: {e.Forts}\n" +
                        $"Taverns unlocked: {e.Taverns}\n" +
                        $"Total Chests Collected: {e.TotalChests}";
                });
            }
        }

        // ==========FORMAL COMMENT=========
        // Event handler for Game Directory menu item
        // Opens the game directory settings form
        // ==========MY NOTES==============
        // Shows a window where you can set game directories
        [SupportedOSPlatform("windows6.1")]
        private void GameDirectoryMenuItem_Click(object? sender, EventArgs e)
        {
            GameDirectoryForm gameDirectoryForm = new();
            gameDirectoryForm.ShowDialog();
        }

        // ==========FORMAL COMMENT=========
        // Event handler for auto-start menu item state changes
        // Synchronizes checkbox with menu item and saves settings
        // ==========MY NOTES==============
        // Makes sure the checkbox matches the menu item when you click it
        [SupportedOSPlatform("windows6.1")]
        private void AutoStartMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            ToolStripMenuItem? autoStartMenuItem = sender as ToolStripMenuItem;
            autoStartCheckBox.Checked = autoStartMenuItem?.Checked ?? false;
            SaveSettings();
        }

        // ==========FORMAL COMMENT=========
        // Event handler for auto-start checkbox state changes
        // Updates settings and prompts for game directory if needed
        // ==========MY NOTES==============
        // Runs when the auto-start checkbox is checked or unchecked
        // Asks for the game folder if we don't know where it is yet
        [SupportedOSPlatform("windows6.1")]
        private void AutoStartCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (settingsManager.IsLoadingSettings)
            {
                return;
            }

            // Add null check for MainMenuStrip
            if (this.MainMenuStrip == null)
            {
                MessageBox.Show("Menu strip not found.");
                autoStartCheckBox.Checked = false;
                return;
            }
            
            ToolStripComboBox? gameDropdown = this.MainMenuStrip.Items.OfType<ToolStripComboBox>().FirstOrDefault();
            if (gameDropdown == null)
            {
                MessageBox.Show("Game dropdown not found.");
                autoStartCheckBox.Checked = false;
                return;
            }

            string selectedGame = gameDropdown.SelectedItem?.ToString() ?? string.Empty;
            string gameDirectory = settingsManager.GetGameDirectory(selectedGame);

            if (string.IsNullOrEmpty(gameDirectory))
            {
                using FolderBrowserDialog folderBrowserDialog = new();
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    gameDirectoryTextBox.Text = folderBrowserDialog.SelectedPath;
                    // Save the directory to the settings manager
                    settingsManager.SaveDirectory(selectedGame, folderBrowserDialog.SelectedPath);
                    SaveSettings();
                }
                else
                {
                    autoStartCheckBox.Checked = false;
                }
            }
            else
            {
                gameDirectoryTextBox.Text = gameDirectory;
                SaveSettings();
            }
        }

        // ==========FORMAL COMMENT=========
        // Event handler for Stats button clicks
        // Retrieves and displays all game statistics from memory
        // Handles error conditions and different game types
        // ==========MY NOTES==============
        // Gets all the game stats when you click the button
        // Shows them in the label or displays an error message if something goes wrong
        [SupportedOSPlatform("windows6.1")]
        private void PercentageButton_Click(object? sender, EventArgs e)
        {
            if (statsTabPage.Controls["percentageLabel"] is Label percentageLabel)
            {
                if (gameConnectionManager.IsConnected && currentProcess == "AC4BFSP.exe")
                {
                    try
                    {
                        if (gameConnectionManager.GameStats == null)
                        {
                            percentageLabel.Text = "Error: gameStats is not initialized.";
                            return;
                        }

                        var (Percent, PercentFloat, Viewpoints, Myan, Treasure, Fragments, Assassin, Naval, Letters, Manuscripts, Music, Forts, Taverns, TotalChests) = gameConnectionManager.GameStats.GetStats();

                        percentageLabel.Text = $"Completion Percentage: {Percent}%\n" +
                            $"Completion Percentage Exact: {Math.Round(PercentFloat, 2)}%\n" +
                            $"Viewpoints Completed: {Viewpoints}\n" +
                            $"Myan Stones Collected: {Myan}\n" +
                            $"Buried Treasure Collected: {Treasure}\n" +
                            $"AnimusFragments Collected: {Fragments}\n" +
                            $"AssassinContracts Completed: {Assassin}\n" +
                            $"NavalContracts Completed: {Naval}\n" +
                            $"LetterBottles Collected: {Letters}\n" +
                            $"Manuscripts Collected: {Manuscripts}\n" +
                            $"Music Sheets Collected: {Music}\n" +
                            $"Forts Captured: {Forts}\n" +
                            $"Taverns unlocked: {Taverns}\n" +
                            $"Total Chests Collected: {TotalChests}";
                    }
                    catch (Win32Exception ex)
                    {
                        percentageLabel.Text = $"Error: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        percentageLabel.Text = $"Unexpected error: {ex.Message}";
                    }
                }
                else if (gameConnectionManager.IsConnected && currentProcess == "ACS.exe")
                    percentageLabel.Text = "Percentage feature not available for Assassin's Creed Syndicate";
                else
                    percentageLabel.Text = "Not connected to a game";
            }
            else
            {
                MessageBox.Show("The percentage label control was not found.");
            }
        }

        // ==========FORMAL COMMENT=========
        // Event handler for Settings menu item click
        // Toggles the visibility of the settings panel in the UI
        // ==========MY NOTES==============
        // Shows or hides the settings panel when you click the menu item
        [SupportedOSPlatform("windows6.1")]
        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            if (this.Controls["settingsPanel"] is Panel settingsPanel)
            {
                settingsPanel.Visible = !settingsPanel.Visible;
            }
        }

        // ==========FORMAL COMMENT=========
        // Event handler for game selection change in settings
        // Updates directory textbox based on the selected game
        // ==========MY NOTES==============
        // When you pick a different game in settings, this shows the right game folder
        [SupportedOSPlatform("windows6.1")]
        private void SettingsGameDropdown_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox settingsGameDropdown)
                return;

            // Add null check for settingsPanel
            if (this.Controls["settingsPanel"] is not Panel settingsPanel)
                return;

            // Add null check for settingsDirectoryTextBox
            if (settingsPanel.Controls["settingsDirectoryTextBox"] is not TextBox settingsDirectoryTextBox)
                return;

            string selectedGame = settingsGameDropdown.SelectedItem?.ToString() ?? string.Empty;
            settingsDirectoryTextBox.Text = settingsManager.GetGameDirectory(selectedGame);
        }
    }
}
