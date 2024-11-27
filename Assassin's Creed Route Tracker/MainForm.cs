using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Assassin_s_Creed_Route_Tracker
{
    public unsafe partial class MainForm : Form
    {
        private IntPtr processHandle;
        private string currentProcess;
        private IntPtr baseAddress;

        private int[] percentPtrOffsets = { 0x49D9774, 0x284 };
        private int[] viewpointsPtrOffsets = { 0x0002E8D0, 0x1A8, 0x28, 0x18 };
        private int[] myanPtrOffsets = { 0x0002E8D0, 0x1A8, 0x3C, 0x18 };
        private int[] treasurePtrOffsets = { 0x0051D814, 0x78, 0x0, 0x678 };
        private int[] fragmentsPtrOffsets = { 0x0002E8D0, 0x1A8, 0x0, 0x18 };
        private int[] waterChestsPtrOffsets = { 0x0002E8D0, 0x1A8, 0x64, 0x18 };
        private int[] unchartedChestsPtrOffsets = { 0x0153A9DC, 0x158, 0x654, 0x18 };
        private int[] assassinPtrOffsets = { 0x0153A9DC, 0xD4, 0x500, 0x78 };
        private int[] navalPtrOffsets = { 0x0002E8D0, 0x1A8, 0x168, 0x18 };
        private int[] lettersPtrOffsets = { 0x014218E8, 0x140, 0x678 };
        private int[] manuscriptsPtrOffsets = { 0x0051D814, 0x6C, 0xA14, 0x18 };
        private int[] musicPtrOffsets = { 0x016B6A7C, 0x54, 0x58C, 0x18 };

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Assassin's Creed Route Tracker";
            this.BackColor = System.Drawing.Color.Black;
            this.ForeColor = System.Drawing.Color.White;

            Label connectionLabel = new Label();
            connectionLabel.Text = "Not connected";
            connectionLabel.Location = new System.Drawing.Point((this.ClientSize.Width - 100) / 2, 10);
            connectionLabel.AutoSize = true;
            this.Controls.Add(connectionLabel);

            ComboBox gameDropdown = new ComboBox();
            gameDropdown.Items.AddRange(new object[] { "", "Assassin's Creed 4", "Assassin's Creed Syndicate" });
            gameDropdown.SelectedIndex = 0;
            gameDropdown.Location = new System.Drawing.Point((this.ClientSize.Width - 800) / 2, 10);
            this.Controls.Add(gameDropdown);

            Button connectButton = new Button();
            connectButton.Text = "Connect to Game";
            connectButton.Location = new System.Drawing.Point((this.ClientSize.Width - 800) / 2, 40);
            connectButton.Click += ConnectButton_Click;
            this.Controls.Add(connectButton);

            Button percentageButton = new Button();
            percentageButton.Text = "Stats";
            percentageButton.Location = new System.Drawing.Point((this.ClientSize.Width - 100) / 2, 140);
            percentageButton.Click += PercentageButton_Click;
            this.Controls.Add(percentageButton);

            Label percentageLabel = new Label();
            percentageLabel.Text = "";
            percentageLabel.Location = new System.Drawing.Point((this.ClientSize.Width - 100) / 2, 180);
            percentageLabel.AutoSize = true;
            this.Controls.Add(percentageLabel);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ComboBox gameDropdown = (ComboBox)this.Controls[1];
            Label connectionLabel = (Label)this.Controls[0];

            string selectedGame = gameDropdown.SelectedItem.ToString();

            if (selectedGame == "Assassin's Creed 4")
                currentProcess = "AC4BFSP.exe";
            else if (selectedGame == "Assassin's Creed Syndicate")
                currentProcess = "ACS.exe";
            else
            {
                connectionLabel.Text = "Please select a game.";
                return;
            }

            Connect();

            if (processHandle != IntPtr.Zero)
                connectionLabel.Text = $"Connected to {selectedGame}";
            else
                connectionLabel.Text = "Error: Cannot connect to process. Make sure the game is running.";
        }

        private void PercentageButton_Click(object sender, EventArgs e)
        {
            Label percentageLabel = (Label)this.Controls[4];

            if (processHandle != IntPtr.Zero && currentProcess == "AC4BFSP.exe")
            {
                try
                {
                    int percent = Read<int>((nint)baseAddress, percentPtrOffsets);
                    int viewpoints = Read<int>((nint)baseAddress, viewpointsPtrOffsets);
                    int myan = Read<int>((nint)baseAddress, myanPtrOffsets);
                    int treasure = Read<int>((nint)baseAddress, treasurePtrOffsets);
                    int fragments = Read<int>((nint)baseAddress, fragmentsPtrOffsets);
                    int waterChests = Read<int>((nint)baseAddress, waterChestsPtrOffsets);
                    int unchartedChests = Read<int>((nint)baseAddress, unchartedChestsPtrOffsets);
                    int assassin = Read<int>((nint)baseAddress, assassinPtrOffsets);
                    int naval = Read<int>((nint)baseAddress, navalPtrOffsets);
                    int letters = Read<int>((nint)baseAddress, lettersPtrOffsets);
                    int manuscripts = Read<int>((nint)baseAddress, manuscriptsPtrOffsets);
                    int music = Read<int>((nint)baseAddress, musicPtrOffsets);

                    percentageLabel.Text = $"Completion Percentage: {percent}%\n" +
                        $"Viewpoints Completed: {viewpoints}\n" +
                        $"Myan Stones Collected: {myan}\n" +
                        $"Buried Treasure Collected: {treasure}\n" +
                        $"AnimusFragments Collected: {fragments}\n" +
                        $"WaterChests Collected: {waterChests}\n" +
                        $"UnchatredChests Collected: {unchartedChests}\n" +
                        $"AssassinContracts Completed: {assassin}\n" +
                        $"NavalContracts Completed: {naval}\n" +
                        $"LetterBottles Collected: {letters}\n" +
                        $"Manuscripts Collected: {manuscripts}\n" +
                        $"Music Sheets Collected: {music}";
                }
                catch (Win32Exception ex)
                {
                    percentageLabel.Text = $"Error: {ex.Message}";
                }
            }
            else if (processHandle != IntPtr.Zero && currentProcess == "ACS.exe")
                percentageLabel.Text = "Percentage feature not available for Assassin's Creed Syndicate";
            else
                percentageLabel.Text = "Not connected to a game";
        }

        private void Connect()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(currentProcess.Replace(".exe", ""));
                if (processes.Length > 0)
                {
                    Process process = processes[0];
                    processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
                    baseAddress = process.MainModule.BaseAddress;
                }
                else
                {
                    processHandle = IntPtr.Zero;
                    baseAddress = IntPtr.Zero;
                }
            }
            catch (Exception)
            {
                processHandle = IntPtr.Zero;
                baseAddress = IntPtr.Zero;
            }
        }

        private T Read<T>(nint baseAddress, int[] offsets) where T : unmanaged
        {
            nint deref = baseAddress;

            foreach (int offset in offsets)
            {
                if (!ReadProcessMemory(processHandle, deref, &deref, sizeof(nint), out nint bytesReadOuter)
                    || bytesReadOuter != sizeof(nint))
                {
                    throw new Win32Exception();
                }

                deref += offset;
            }

            T result;
            if (!ReadProcessMemory(processHandle, deref, &result, sizeof(T), out nint bytesReadInner)
                || bytesReadInner != sizeof(T))
            {
                throw new Win32Exception();
            }

            return result;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(
            nint hProcess,
            nint lpBaseAddress,
            void* lpBuffer,
            nint nSize,
            out nint lpNumberOfBytesRead);

        private const int PROCESS_WM_READ = 0x0010;
    }
}
