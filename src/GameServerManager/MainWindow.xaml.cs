using PrimS.Telnet;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace GameServerManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Constants

        private const string SERVER_DIRECTORY = @"E:\GameServers\Empyrion\Server\";
        private const string SERVER_FILE_LOCATION = @"E:\GameServers\Empyrion\Server\EmpyrionDedicated_Custom.cmd";
        private const string EMPYRION_PROCESS_NAME = @"EmpyrionDedicated";

        private const string TELNET_ADDRESS = @"127.0.0.1";
        private const int TELNET_PORT = 30004;
        private const string TELNET_USERNAME = @"";
        private const string TELNET_PASSWORD = @"XXXXX";

        private const string GAME_SAVE_DATA_DIRECTORY = @"E:\GameServers\Empyrion\SaveData\Games\BlargHonk\";
        private const string GAME_BACKUP_TEMP_DIRECTORY = @"E:\GameServers\Empyrion\SaveDataBackups\temp\";
        private const string GAME_BACKUP_DIRECTORY = @"E:\GameServers\Empyrion\SaveDataBackups\";

        private const int BACKUP_FREQUENCY_MS = 600000;

        private const string ZIP_PREFIX = "SaveData.Games.BlargHonk";

        #endregion

        #region Backing Fields

        private bool _isServerRunning;
        private bool _isTelnetConnected;
        private bool _isAutoBackingUp;

        private Client? _telnetClient;

        private readonly Timer _backupTimer = new Timer(BACKUP_FREQUENCY_MS);
        private bool _isBackingUp;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CheckServerStatus();

            _backupTimer.Elapsed += Timer_Elapsed;
        }

        #region Bindings

        public bool IsServerRunning
        {
            get
            {
                CheckServerStatus();
                return _isServerRunning;
            }
            set => SetProperty(ref _isServerRunning, value);
        }

        public bool IsTelnetConnected
        {
            get => _isTelnetConnected;
            set => SetProperty(ref _isTelnetConnected, value);
        }

        public bool IsAutoBackingUp
        {
            get => _isAutoBackingUp;
            set => SetProperty(ref _isAutoBackingUp, value);
        }

        #endregion

        #region Properties

        private string ZipFileName => $"{ZIP_PREFIX} - {DateTime.Now.ToTimeStamp()}.zip";

        #endregion

        #region Buttons

        private void StartStopServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsServerRunning)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }

            CheckServerStatus();
        }

        private void TelnetConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsServerRunning)
            {
                return;
            }

            //if (IsTelnetConnected)
            //{
            //    DisconnectFromTelnet();
            //}
            //else
            //{
            //    IsTelnetConnected = ConnectToTelnet().Result;
            //}
        }

        private void ToggleBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsAutoBackingUp)
            {
                EndBackups();
            }
            else
            {
                BeginBackups();
            }

            IsAutoBackingUp = !IsAutoBackingUp;
        }

        #endregion

        #region Methods

        private void StartServer()
        {
            using var serverProcess = new Process();
            serverProcess.StartInfo.WorkingDirectory = SERVER_DIRECTORY;
            serverProcess.StartInfo.FileName = SERVER_FILE_LOCATION;
            serverProcess.StartInfo.UseShellExecute = true;
            _ = serverProcess.Start();
            serverProcess.WaitForExit();
        }

        private void StopServer()
        {

        }

        private void CheckServerStatus()
        {
            var serverProcess = Process.GetProcessesByName(EMPYRION_PROCESS_NAME);
            var isRunning = serverProcess.Any();
            IsServerRunning = isRunning;
        }

        //private async Task<bool> ConnectToTelnet()
        //{
        //    _telnetClient = new Client(TELNET_ADDRESS, TELNET_PORT, new System.Threading.CancellationToken());
        //    return await _telnetClient.TryLoginAsync(TELNET_USERNAME, TELNET_PASSWORD, 10000);            
        //}

        //private async void DisconnectFromTelnet()
        //{

        //}

        private void BeginBackups()
        {
            _backupTimer.Start();
        }

        private void EndBackups()
        {
            _backupTimer.Stop();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isBackingUp)
            {
                return;
            }

            _isBackingUp = true;

            Debug.WriteLine("Backing Up Files...");

            Debug.WriteLine("Copying Files...");
            CopyFilesRecursively(GAME_SAVE_DATA_DIRECTORY, GAME_BACKUP_TEMP_DIRECTORY);

            Debug.WriteLine("Zipping Files...");
            var zipFilePath = Path.Combine(GAME_BACKUP_DIRECTORY, ZipFileName);
            ZipFile.CreateFromDirectory(GAME_BACKUP_TEMP_DIRECTORY, zipFilePath);

            Debug.WriteLine($"Done, backup saved to {zipFilePath}");
            _isBackingUp = false;
        }

        private static void CopyFilesRecursively(string sourceDirectory, string destinationDirectory)
        {
            if (Directory.Exists(destinationDirectory))
            {
                Directory.Delete(destinationDirectory, true);
            }

            _ = Directory.CreateDirectory(destinationDirectory);

            var directories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);
            foreach (var directoryPath in directories)
            {
                _ = Directory.CreateDirectory(directoryPath.Replace(sourceDirectory, destinationDirectory));
            }

            var files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                FileCopyWithInUseFallBack(filePath, filePath.Replace(sourceDirectory, destinationDirectory));
            }
        }

        private static void FileCopyWithInUseFallBack(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                File.Copy(sourceFilePath, destinationFilePath);
            }
            catch (IOException e)
            {
                if (!e.Message.Contains("in use"))
                {
                    throw;
                }

                var cmdProcess = new Process();
                cmdProcess.StartInfo.UseShellExecute = false;
                cmdProcess.StartInfo.RedirectStandardOutput = true;
                cmdProcess.StartInfo.FileName = "cmd.exe";
                //cmdProcess.StartInfo.Arguments = "/C copy \"" + sourceFilePath + "\" \"" + destinationFilePath + "\"";
                cmdProcess.StartInfo.Arguments = $"/C copy \"{sourceFilePath}\" \"{destinationFilePath}\"";
                _ = cmdProcess.Start();
                Debug.WriteLine(cmdProcess.StandardOutput.ReadToEnd());
                cmdProcess.WaitForExit();
                cmdProcess.Close();
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
