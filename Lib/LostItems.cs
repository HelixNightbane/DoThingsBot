using Decal.Adapter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VirindiViewService.Controls;

namespace DoThingsBot.Lib {
    public static class LostItems {
        private static DateTime lastThought = DateTime.UtcNow;
        private const int THINK_INTERVAL = 100;
        private const int SCANS_PER_THINK = 10;
        private static bool isDone = true;
        private static int lostItemCount = 0;
        private static List<string> playerNamesToScan = new List<string>();
        private static List<string> playersMissingItems = new List<string>();
        private static bool needsInitialScan = true;

        public static void ScanAll() {
            ((HudList)Globals.MainView.view["UILogsLostItemsList"]).ClearRows();
            isDone = false;
            lostItemCount = 0;
            playersMissingItems = new List<string>();
            playerNamesToScan = GetCharactersToScan();

            Util.WriteToChat($"Scanning {playerNamesToScan.Count} player data files for lost items, this will take about {Util.GetFriendlyTimeDifference((ulong)(((float)playerNamesToScan.Count / SCANS_PER_THINK) * THINK_INTERVAL)/1000)}");
        }

        private static List<string> GetCharactersToScan() {
            var characterNames = new List<string>();

            DirectoryInfo d = new DirectoryInfo(Util.GetPlayerDataDirectory());
            FileInfo[] fileList = d.GetFiles("*.json");

            foreach (FileInfo file in fileList) {
                characterNames.Add(file.Name.Replace(".json", ""));
            }

            return characterNames;
        }

        public static void ScanCharacterDataForLostItems(string playerName) {
            ItemBundle bundle = new ItemBundle(playerName);
            
            if (bundle != null) {
                foreach (var id in bundle.GetStolenItems()) {
                    var wo = CoreManager.Current.WorldFilter[id];
                    if (wo == null) continue;

                    lostItemCount++;
                    if (!playersMissingItems.Contains(playerName)) {
                        playersMissingItems.Add(playerName);
                    }

                    HudList.HudListRowAccessor newRow = ((HudList)Globals.MainView.view["UILogsLostItemsList"]).AddRow();
                    ((HudStaticText)newRow[0]).Text = playerName;
                    ((HudPictureBox)newRow[1]).Image = wo.Icon + 0x6000000;
                    ((HudStaticText)newRow[2]).Text = Util.GetGameItemDisplayName(wo);
                }
            }
        }

        public static void Think() {
            if (needsInitialScan) {
                needsInitialScan = false;
                ScanAll();
                return;
            }

            if (isDone) return;

            if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(THINK_INTERVAL)) {
                lastThought = DateTime.UtcNow;

                if (playerNamesToScan.Count > 0) {
                    var i = 0;
                    while (playerNamesToScan.Count > 0 && i < SCANS_PER_THINK) {
                        var characterName = playerNamesToScan[0];
                        playerNamesToScan.RemoveAt(0);
                        ScanCharacterDataForLostItems(characterName);
                        i++;
                    }
                }
                else {
                    isDone = true;
                    Util.WriteToChat($"Done scanning, found {lostItemCount} lost items from {playersMissingItems.Count} players.");
                }
            }
        }
    }
}
