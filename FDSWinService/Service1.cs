using System.IO;
using System;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace FDSWinService
{
    public partial class ServiceFDS : ServiceBase
    {
        private const string PipeName = @"\\.\pipe\MyAppPipe";
        private Timer checkLauncherTimer;
        private Timer checkExeTimer;

        public ServiceFDS()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("Service Service at " + DateTime.Now);
            checkLauncherTimer = new Timer(CheckLauncherStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            //checkExeTimer = new Timer(checkExeStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private void checkExeStatus(object state)
        {
            WriteLog("Checking EXE Service at " + DateTime.Now);
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string originalPath = Path.GetDirectoryName(exePath);
            //string originalPath = AppDomain.CurrentDomain.BaseDirectory; // Path where fds.exe should exist
            string backupPath = Path.Combine("C:\\Program Files (x86)", "FDS"); // Path to the backup files

            string executablePath = Path.Combine(originalPath, "fds.exe");

            // Check if fds.exe exists in the original path
            if (!File.Exists(executablePath))
            {
                WriteLog("fds.exe not found. Copying files from backup... " + DateTime.Now);
                 
                // Copy files and folder from the backup path to the original path
                try
                {
                    Directory.CreateDirectory(originalPath);
                    CopyFilesRecursively(new DirectoryInfo(backupPath), new DirectoryInfo(originalPath));
                    WriteLog("Files copied successfully. " + DateTime.Now);
                     
                }
                catch (Exception ex)
                {
                    WriteLog("Error copying files: . " + DateTime.Now);
                   
                }
            }
            else
            {
                WriteLog("fds.exe found in the original path. " + DateTime.Now);
                 
            }
        }

        private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                DirectoryInfo newDir = new DirectoryInfo(Path.Combine(target.FullName, dir.Name));
                if (!newDir.Exists) // Check if directory already exists in the target directory
                {
                    newDir.Create(); // Create the directory if it doesn't exist
                    CopyFilesRecursively(dir, newDir);
                }
                else
                {
                    // Directory already exists, continue processing other directories
                }
            }
            foreach (FileInfo file in source.GetFiles())
            {
                string filePath = Path.Combine(target.FullName, file.Name);
                if (!File.Exists(filePath)) // Check if file already exists in the target directory
                {
                    file.CopyTo(filePath, false); // Don't overwrite existing files
                }
            }
        }

        private void CheckLauncherStatus(object state)
        {
            WriteLog("Checking Launcher Service at " + DateTime.Now);
            bool launcherRunning = IsLauncherRunning();

            if (launcherRunning)
            {
                WriteLog("Launcher running at " + DateTime.Now);
                SendCommandToLauncher("StartUI");
            }
            else
            {
                WriteLog("Launcher not runnning at " + DateTime.Now);
            }
        }

        private bool IsLauncherRunning()
        {
            string launcherProcessName = "FDS_LauncherApp"; // Replace with your actual launcher process name

            Process[] processes = Process.GetProcessesByName(launcherProcessName);

            return processes.Length > 0;
        }

        private void SendCommandToLauncher(string command)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                pipeClient.Connect(); // Connect to the service
                // Send a message/command to the service
                byte[] buffer = Encoding.UTF8.GetBytes(command);
                pipeClient.Write(buffer, 0, buffer.Length);
            }
        }
        protected override void OnStop()
        {
            ServiceFDS service = new ServiceFDS();
            service.SendStopSignalToConsoleApp();
        }

        public void SendStopSignalToConsoleApp()
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect();
                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        writer.Write("StopSignal"); // Send a stop signal to the console app
                    }
                }

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }


        private void WriteLog(string logMessage)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "Logs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, "ServiceLog_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

                using (StreamWriter streamWriter = File.AppendText(filePath))
                {
                    streamWriter.WriteLine($"{DateTime.Now} - {logMessage}");
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
