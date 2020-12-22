using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

namespace BlazorApp1.Data
{
    public class SaveOption
    {
        public string Choice { get; set; }
        public string TimeOfSave { get; set; }
        public string Ticks { get; set; }
    }

    public class NoitaHub : Hub
    {
        public const string HubUrl = "/noitahub";

        private readonly NoitaSaveScummer _noitaSaveScummer;

        public NoitaHub(NoitaSaveScummer noitaSaveScummer)
        {
            _noitaSaveScummer = noitaSaveScummer;
        }

        public List<SaveOption> GetSaveMenu()
        {
            return _noitaSaveScummer.WebFriendlySaveMenu();
        }

        public bool NoitaProcessExists()
        {
            return _noitaSaveScummer.NoitaProcessExists();
        }

        public string SaveGameData()
        {
            return _noitaSaveScummer.SaveGameData();
        }

        public string LoadGameData(string saveChoice)
        {
            return _noitaSaveScummer.LoadGameData(saveChoice);
        }

        public string StartNoita()
        {
            return _noitaSaveScummer.StartNoita();
        }
    }
}
