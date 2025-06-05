using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using Route_Tracker.Properties;

namespace Route_Tracker
{
    // ==========FORMAL COMMENT=========
    // Main form class that provides the user interface for the Assassin's Creed Route Tracker
    // Handles connections to game processes, memory reading, and displaying game statistics
    // ==========MY NOTES==============
    // This is the main window of the app - it does everything from connecting to the game
    // to showing the stats and managing settings
    public partial class MainForm : Form
    {
        // Theme class for consistent styling throughout the application
        private static class AppTheme
        {
            // Colors
            public static readonly Color BackgroundColor = Color.Black;
            public static readonly Color TextColor = Color.White;
            public static readonly Color AccentColor = Color.DarkRed; // Good for highlights/buttons

            // Fonts
            public static readonly Font DefaultFont = new("Segoe UI", 9f);
            public static readonly Font HeaderFont = new("Segoe UI", 12f, FontStyle.Bold);
            public static readonly Font StatsFont = new("Segoe UI", 14f);

            // Spacing
            public const int StandardPadding = 10;
            public const int StandardMargin = 5;

            // Apply theme to a control
            public static void ApplyTo(Control control)
            {
                control.BackColor = BackgroundColor;
                control.ForeColor = TextColor;
                control.Font = DefaultFont;
            }
        }

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

        [SupportedOSPlatform("windows6.1")]
        private void InitializeCustomComponents()
        {
            this.Text = "Route Tracker";
            AppTheme.ApplyTo(this);

            CreateMainMenu();
            SetupTabs();
            InitializeHiddenControls();
        }

        [SupportedOSPlatform("windows6.1")]
        private void CreateMainMenu()
        {
            // Create and configure the MenuStrip
            MenuStrip menuStrip = new()
            {
                Dock = DockStyle.Top,
                BackColor = AppTheme.BackgroundColor,
                ForeColor = AppTheme.TextColor
            };

            // Create settings menu with sub-items
            CreateSettingsMenu(menuStrip);

            // Create tab navigation buttons
            CreateTabButtons(menuStrip);

            // Create connection controls
            CreateConnectionControls(menuStrip);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        [SupportedOSPlatform("windows6.1")]
        private void CreateSettingsMenu(MenuStrip menuStrip)
        {
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
        }

        [SupportedOSPlatform("windows6.1")]
        private void CreateTabButtons(MenuStrip menuStrip)
        {
            // Create and configure the Stats tab button
            ToolStripButton statsTabButton = new("Stats");
            statsTabButton.Click += (sender, e) => tabControl.SelectedTab = statsTabPage;
            menuStrip.Items.Add(statsTabButton);

            // Create and configure the Route tab button
            ToolStripButton routeTabButton = new("Route");
            routeTabButton.Click += (sender, e) => tabControl.SelectedTab = routeTabPage;
            menuStrip.Items.Add(routeTabButton);
        }

        [SupportedOSPlatform("windows6.1")]
        private void CreateConnectionControls(MenuStrip menuStrip)
        {
            // Create and configure the connection label
            ToolStripLabel connectionLabel = new()
            {
                Text = "Not connected"
            };
            menuStrip.Items.Add(connectionLabel);

            // Create and configure the game dropdown
            ToolStripComboBox gameDropdown = new();
            gameDropdown.Items.AddRange(["", "Assassin's Creed 4", "God of War 2018"]);
            gameDropdown.SelectedIndex = 0;
            menuStrip.Items.Add(gameDropdown);

            // Create and configure the connect button
            ToolStripButton connectButton = new("Connect to Game");
            connectButton.Click += ConnectButton_Click;
            menuStrip.Items.Add(connectButton);
        }

        [SupportedOSPlatform("windows6.1")]
        private void SetupTabs()
        {
            // Create and configure the TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            AppTheme.ApplyTo(tabControl);

            // Create the tabs
            CreateStatsTab();
            CreateRouteTab();

            // Add the TabPages to the TabControl
            tabControl.TabPages.Add(statsTabPage);
            tabControl.TabPages.Add(routeTabPage);

            // Add the TabControl to the form's controls
            this.Controls.Add(tabControl);
        }

        [SupportedOSPlatform("windows6.1")]
        private void CreateStatsTab()
        {
            // Create and configure the Stats TabPage
            statsTabPage = new TabPage("Stats");
            AppTheme.ApplyTo(statsTabPage);

            // Use TableLayoutPanel for better layout management
            TableLayoutPanel statsLayout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,  // Left margin, content, right margin
                RowCount = 2,     // Button row, stats display row
                Padding = new Padding(AppTheme.StandardPadding)
            };
            AppTheme.ApplyTo(statsLayout);

            // Configure column styles - center content with margins on sides
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));   // Left margin
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90F));  // Content
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));   // Right margin

            // Set row styles
            statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));           // Button row
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));      // Stats display

            // Top section for the button - but in a FlowLayoutPanel to center it
            FlowLayoutPanel buttonPanel = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };
            AppTheme.ApplyTo(buttonPanel);

            // Create button with proper size
            Button percentageButton = new()
            {
                Text = "Stats",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, AppTheme.StandardMargin)
            };
            percentageButton.Click += PercentageButton_Click;
            buttonPanel.Controls.Add(percentageButton);

            // Add button panel to layout in the center column of the first row
            statsLayout.Controls.Add(buttonPanel, 1, 0);

            // Bottom section for the stats display - in the center column
            Label percentageLabel = new()
            {
                Name = "percentageLabel",
                Text = "",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = AppTheme.StatsFont
            };
            statsLayout.Controls.Add(percentageLabel, 1, 1);

            // Add layout to the tab
            statsTabPage.Controls.Add(statsLayout);
        }

        [SupportedOSPlatform("windows6.1")]
        private void CreateRouteTab()
        {
            // Create and configure the Route TabPage
            routeTabPage = new TabPage("Route");
            routeTabPage.BackColor = Color.Black;

            // Create the DataGridView for route entries
            var routeGrid = CreateRouteGridView();

            // Add the grid to the tabpage
            routeTabPage.Controls.Add(routeGrid);

            // Load route data - this could happen after routeManager is initialized in ConnectButton_Click
            LoadRouteData(routeGrid);
        }

        private DataGridView CreateRouteGridView()
        {
            // Create a DataGridView for displaying route entries
            DataGridView routeGrid = new DataGridView
            {
                Name = "routeGrid",
                Dock = DockStyle.Fill,
                BackgroundColor = Color.Black,
                ForeColor = Color.White,
                GridColor = Color.FromArgb(80, 80, 80),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 11f),
                RowTemplate = { Height = 30 },
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.Single
            };

            ConfigureRouteGridColumns(routeGrid);
            ApplyRouteGridStyling(routeGrid);

            return routeGrid;
        }

        private void ConfigureRouteGridColumns(DataGridView grid)
        {
            // Configure columns - with item first, completion status second
            DataGridViewTextBoxColumn itemColumn = new DataGridViewTextBoxColumn
            {
                Name = "Item",
                HeaderText = "", // No header text
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(10, 5, 5, 5),
                    BackColor = Color.Black,
                    ForeColor = Color.White
                }
            };

            // Use text column for completion status
            DataGridViewTextBoxColumn completedColumn = new DataGridViewTextBoxColumn
            {
                Name = "Completed",
                HeaderText = "",
                Width = 50,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    BackColor = Color.Black,
                    ForeColor = Color.White
                }
            };

            // Add columns in correct order
            grid.Columns.Add(itemColumn);
            grid.Columns.Add(completedColumn);
        }

        private void ApplyRouteGridStyling(DataGridView grid)
        {
            // Apply theme colors to the grid
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.RowHeadersDefaultCellStyle.BackColor = Color.Black;
            grid.RowsDefaultCellStyle.BackColor = Color.Black;
            grid.RowsDefaultCellStyle.ForeColor = Color.White;

            // No alternating row colors
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.Black;

            // Make the row selection very subtle
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 30, 35);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;

            // Make the header row minimal height or invisible
            grid.ColumnHeadersHeight = 4;
            grid.ColumnHeadersVisible = false;

            // Explicitly set scrollbar visibility
            grid.ScrollBars = ScrollBars.Vertical;
        }

        private void LoadRouteData(DataGridView routeGrid)
        {
            // If routeManager is initialized, let it handle loading
            if (routeManager != null)
            {
                string selectedGame = GetSelectedGameName();
                if (!string.IsNullOrEmpty(selectedGame))
                {
                    routeManager.LoadRouteDataIntoGrid(routeGrid, selectedGame);
                }
                else
                {
                    // Show a placeholder message when no game is selected yet
                    routeGrid.Rows.Add("Please select and connect to a game to load route data", "");
                }
            }
            else
            {
                // Initial diagnostic message
                routeGrid.Rows.Add("Connect to a game to load route tracking data", "");
            }
        }

        private string GetSelectedGameName()
        {
            if (MainMenuStrip?.Items.OfType<ToolStripComboBox>().FirstOrDefault() is ToolStripComboBox gameDropdown)
            {
                return gameDropdown.SelectedItem?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        // In MainForm.cs, replace the entire UpdateRouteCompletionStatus method with:
        private void UpdateRouteCompletionStatus(DataGridView routeGrid, StatsUpdatedEventArgs stats)
        {
            // Delegate all the route checking logic to the RouteManager
            if (routeManager != null)
            {
                routeManager.UpdateCompletionStatus(routeGrid, stats);
            }
        }

        [SupportedOSPlatform("windows6.1")]
        private void InitializeHiddenControls()
        {
            // Initialize gameDirectoryTextBox and autoStartCheckBox
            gameDirectoryTextBox = new TextBox
            {
                ReadOnly = true,
                Visible = false,
                Width = 600,
                Dock = DockStyle.Top
            };
            statsTabPage.Controls.Add(gameDirectoryTextBox);

            autoStartCheckBox = new CheckBox
            {
                Text = "Auto-Start Game",
                Visible = false,
                Dock = DockStyle.Top
            };
            autoStartCheckBox.CheckedChanged += AutoStartCheckBox_CheckedChanged;
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
            currentProcess = selectedGame switch
            {
                "Assassin's Creed 4" => "AC4BFSP.exe",
                "God of War 2018" => "GoW.exe",
                _ => string.Empty
            };

            // Use GameConnectionManager to handle the connection
            bool connected = await gameConnectionManager.ConnectToGameAsync(selectedGame, autoStartCheckBox.Checked);

            // Use actual route file path when initializing RouteManager
            if (connected)
            {
                connectionLabel.Text = $"Connected to {selectedGame}";
                string routeFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Routes",
                    "AC4 100 % Route - Main Route.tsv"); // Use the exact filename you have

                // Pass the gameConnectionManager when creating the RouteManager
                routeManager = new RouteManager(routeFilePath, gameConnectionManager);

                // Reload route data if we're on the route tab
                if (routeTabPage.Controls["routeGrid"] is DataGridView routeGrid)
                {
                    LoadRouteData(routeGrid);
                }
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
            this.Invoke(() => {
                // Update stats display if in updating mode
                if (statsTabPage.Controls[0] is TableLayoutPanel statsLayout &&
                    statsLayout.Controls["percentageLabel"] is Label percentageLabel &&
                    percentageLabel.Tag?.ToString() == "updating")
                {
                    UpdateStatsDisplay(percentageLabel, e.Percent, e.PercentFloat, e.Viewpoints, e.Myan,
                        e.Treasure, e.Fragments, e.Assassin, e.Naval, e.Letters, e.Manuscripts,
                        e.Music, e.Forts, e.Taverns, e.TotalChests);
                }

                // Update route completion status
                if (routeTabPage.Controls["routeGrid"] is DataGridView routeGrid)
                {
                    UpdateRouteCompletionStatus(routeGrid, e);
                }
            });
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
            if (statsTabPage.Controls[0] is TableLayoutPanel statsLayout &&
            statsLayout.Controls["percentageLabel"] is Label percentageLabel)
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

                        // Get current stats
                        var (Percent, PercentFloat, Viewpoints, Myan, Treasure, Fragments, Assassin, Naval, Letters, Manuscripts, Music, Forts, Taverns, TotalChests) = gameConnectionManager.GameStats.GetStats();

                        // Update the display
                        UpdateStatsDisplay(percentageLabel, Percent, PercentFloat, Viewpoints, Myan,
                            Treasure, Fragments, Assassin, Naval, Letters, Manuscripts,
                            Music, Forts, Taverns, TotalChests);

                        // Set a tag to indicate we're in continuous update mode
                        percentageLabel.Tag = "updating";
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
                else if (gameConnectionManager.IsConnected && currentProcess == "GoW.exe")
                    percentageLabel.Text = "Percentage feature not available for God of War 2018";
                else
                    percentageLabel.Text = "Not connected to a game";
            }
            else
            {
                MessageBox.Show("The percentage label control was not found.");
            }
        }

        // Helper method to update the stats display (used by both button click and event handler)
        private void UpdateStatsDisplay(Label label, int percent, float percentFloat, int viewpoints, int myan,
            int treasure, int fragments, int assassin, int naval, int letters, int manuscripts,
            int music, int forts, int taverns, int totalChests)
        {
            label.Text = $"Completion Percentage: {percent}%\n" +
                $"Completion Percentage Exact: {Math.Round(percentFloat, 2)}%\n" +
                $"Viewpoints Completed: {viewpoints}\n" +
                $"Myan Stones Collected: {myan}\n" +
                $"Buried Treasure Collected: {treasure}\n" +
                $"AnimusFragments Collected: {fragments}\n" +
                $"AssassinContracts Completed: {assassin}\n" +
                $"NavalContracts Completed: {naval}\n" +
                $"LetterBottles Collected: {letters}\n" +
                $"Manuscripts Collected: {manuscripts}\n" +
                $"Music Sheets Collected: {music}\n" +
                $"Forts Captured: {forts}\n" +
                $"Taverns unlocked: {taverns}\n" +
                $"Total Chests Collected: {totalChests}";
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
