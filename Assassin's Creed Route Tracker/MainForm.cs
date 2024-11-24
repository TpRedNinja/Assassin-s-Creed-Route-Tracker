using System.Diagnostics;
using ProcessMemory;

namespace Assassin_s_Creed_Route_Tracker
{
    public partial class MainForm : Form
    {
        private ProcessMemoryHandler processMemoryHandler;
        private Process currentProcess;
        private MultilevelPointer percentPtr;
        private MultilevelPointer percentFloatPtr;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Assassin's Creed Route Tracker";
            this.BackColor = Color.Black;
            this.ForeColor = Color.White;

            Label connectionLabel = new Label();
            connectionLabel.Text = "Not connected";
            connectionLabel.Location = new Point((this.ClientSize.Width - 100) / 2, 10);
            connectionLabel.AutoSize = true;
            this.Controls.Add(connectionLabel);

            ComboBox gameDropdown = new ComboBox();
            gameDropdown.Items.AddRange(new object[] { "", "Assassin's Creed 4", "Assassin's Creed Syndicate" });
            gameDropdown.SelectedIndex = 0;
            gameDropdown.Location = new Point((this.ClientSize.Width - 800) / 2, 10);
            this.Controls.Add(gameDropdown);

            Button connectButton = new Button();
            connectButton.Text = "Connect to Game";
            connectButton.Location = new Point((this.ClientSize.Width - 800) / 2, 40);
            connectButton.Click += ConnectButton_Click;
            this.Controls.Add(connectButton);

            Button percentageButton = new Button();
            percentageButton.Text = "Stats";
            percentageButton.Location = new Point((this.ClientSize.Width - 100) / 2, 140);
            percentageButton.Click += PercentageButton_Click;
            this.Controls.Add(percentageButton);

            Label percentageLabel = new Label();
            percentageLabel.Text = "";
            percentageLabel.Location = new Point((this.ClientSize.Width - 100) / 2, 180);
            percentageLabel.AutoSize = true;
            this.Controls.Add(percentageLabel);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ComboBox gameDropdown = (ComboBox)this.Controls[1];
            Label connectionLabel = (Label)this.Controls[0];
            string processName;
            string? selectedGame = gameDropdown.SelectedItem?.ToString();

            if (selectedGame == "Assassin's Creed 4")
                processName = "AC4BFSP";
            else if (selectedGame == "Assassin's Creed Syndicate")
                processName = "ACS";
            else
            {
                connectionLabel.Text = "Please select a game.";
                return;
            }

            Connect(processName);
            if (processMemoryHandler != null)
                connectionLabel.Text = $"Connected to {selectedGame}";
            else
                connectionLabel.Text = "Error: Cannot connect to process. Make sure the game is running.";
        }

        private void PercentageButton_Click(object sender, EventArgs e)
        {
            Label percentageLabel = (Label)this.Controls[4];
            if (processMemoryHandler != null && currentProcess.ProcessName == "AC4BFSP")
            {
                try
                {
                    int percent = percentPtr.DerefInt(0x284);
                    float percentFloat = percentFloatPtr.DerefInt(0x74);
                    percentageLabel.Text = $"Completion Percentage: {percent}%\n" +
                        $"Float Percentage: {percentFloat:F5}";

                }
                catch (Exception ex)
                {
                    percentageLabel.Text = $"Error: {ex.Message}";
                }
            }
            else if (processMemoryHandler != null && currentProcess.ProcessName == "ACS")
                percentageLabel.Text = "Percentage feature not available for Assassin's Creed Syndicate";
            else
                percentageLabel.Text = "Not connected to a game";
        }

        private unsafe void Connect(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    processMemoryHandler = null;
                    percentPtr = null;
                    percentFloatPtr = null;
                    return;
                }

                currentProcess = processes[0];
                processMemoryHandler = new ProcessMemoryHandler((uint)currentProcess.Id);
                if (processMemoryHandler != null && currentProcess.ProcessName == "AC4BFSP")
                {
                    // Set up the percentage pointer for AC4
                    percentPtr = new MultilevelPointer(processMemoryHandler, (nint*)currentProcess.MainModule?.BaseAddress, 0x49D9774);
                    percentFloatPtr = new MultilevelPointer(processMemoryHandler, (nint*)currentProcess.MainModule?.BaseAddress, 0x049F1EE8);
                }
            }
            catch (Exception)
            {
                processMemoryHandler = null;
                percentPtr = null;
                percentFloatPtr = null;
            }
        }
    }
}
