using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace BlazorApp1.Data
{
    public class NoitaSaveScummer
    {
        private Process _noitaProcess = null;
        private int _waitTime = 10000; //in milliseconds
        private string _appDataPath;
        private string _noitaAppDataPath;
        public string _noitaSavePath;
        public string _noitaScumPath;
        private string _steamPath;
        private string _steamGameIdPath;
        private Timer _noitaProcessTimer;
        private IHubContext<NoitaHub> _hubContext;
        
        public NoitaSaveScummer(IHubContext<NoitaHub> hubContext)
        {
            _hubContext = hubContext;
            _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _noitaAppDataPath = _appDataPath + @"Low\Nolla_Games_Noita\";
            _noitaSavePath = _noitaAppDataPath + "save00";
            _noitaScumPath = _noitaAppDataPath + @"scum\";
            _steamPath = @"C:\Program Files (x86)\Steam\steam.exe";
            _steamGameIdPath = "steam://rungameid/881100";

            _noitaProcessTimer = new Timer(_waitTime);
            _noitaProcessTimer.Elapsed += TryAttachExitEvent;
            _noitaProcessTimer.AutoReset = true;
            _noitaProcessTimer.Enabled = true;
            TryAttachExitEvent(null, null);

            _hubContext.Clients.All.SendAsync("GetSaveMenu", WebFriendlySaveMenu());
        }

        public string StartNoita()
        {
            if(!NoitaProcessExists())
            {
                Process.Start(_steamPath, _steamGameIdPath);
                return "Noita Started";
            }
            return "Noita is already running)";
        }

        public Dictionary<string, DirectoryInfo> GetSaveMenu()
        {
            string[] saveDirs = Directory.GetDirectories(_noitaScumPath);
            Dictionary<string, DirectoryInfo> saveMenu = new Dictionary<string, DirectoryInfo>();
            int counter = 1;
            foreach (string saveDir in saveDirs)
            {
                saveMenu.Add(counter.ToString(), new DirectoryInfo(saveDir));
                counter++;
            }
            saveMenu.OrderBy(x => x.Value.LastWriteTime);
            return saveMenu;
        }

        public List<SaveOption> WebFriendlySaveMenu()
        {
            List<SaveOption> result = new List<SaveOption>();
            foreach (var item in GetSaveMenu())
            {
                var tmp = new SaveOption();
                tmp.Choice = item.Key;
                tmp.TimeOfSave = item.Value.Name.Replace("save00-", "");
                tmp.Ticks = item.Value.LastWriteTime.Ticks.ToString();
                result.Add(tmp);
            }
            return result;
        }

        public bool NoitaProcessExists()
        {
            if (_noitaProcess != null)
            {
                _hubContext.Clients.All.SendAsync("UpdateProcessExists", true);
                return true;
            }

            Process[] localByName = Process.GetProcessesByName("noita");
            if (localByName.Any())
            {
                _noitaProcess = localByName[0];
                _hubContext.Clients.All.SendAsync("UpdateProcessExists", true);
                return true;
            }

            _hubContext.Clients.All.SendAsync("UpdateProcessExists", false);
            return false;
        }

        void TryAttachExitEvent(Object source, ElapsedEventArgs e)
        {
            if (NoitaProcessExists())
            {
                Console.WriteLine("Noita process found, setting up Exited delegate.");
                _noitaProcess.EnableRaisingEvents = true;
                _noitaProcess.Exited += new EventHandler(On_Noita_Exited);
                _noitaProcessTimer.Stop();
            }
            else
                Console.WriteLine(string.Format("Noita isn't running yet, unable to watch for exit. Waiting {0} milliseconds before trying again.", _waitTime));
        }


        void On_Noita_Exited(object sender, EventArgs e)
        {
            _noitaProcess = null;
            _hubContext.Clients.All.SendAsync("NoitaExited", "Noita Exited.");
            _noitaProcessTimer.Start();
        }

        public string LoadGameData(string saveChoice)
        {
            if (!GetSaveMenu().ContainsKey(saveChoice))
                return "Invalid Option.";
            if (NoitaProcessExists())
                return "Noita is still running, please exit first.";
            DirectoryCopy(GetSaveMenu()[saveChoice].FullName, _noitaSavePath, true, true);
            string response = StartNoita();
            Console.WriteLine(response);
            return "Data Loaded.";
        }

        public string SaveGameData()
        {
            string newUniqueSavePath = _noitaScumPath + "save00-" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_tt");
            DirectoryCopy(_noitaSavePath, newUniqueSavePath, true, false);
            _hubContext.Clients.All.SendAsync("GetSaveMenu", WebFriendlySaveMenu());
            return "Game Data Saved.";
        }

        public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, overwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs, overwrite);
                }
            }
        }

    }
}
