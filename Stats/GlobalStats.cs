using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DoThingsBot.Stats {
    class GlobalStats {
        public Dictionary<string, int> salvageBagsUsed = new Dictionary<string, int>();
        public Dictionary<string, int> nonImbuesFailed = new Dictionary<string, int>();
        public Dictionary<string, int> imbuesLandedBySalvageType = new Dictionary<string, int>();
        public Dictionary<string, int> imbuesFailedBySalvageType = new Dictionary<string, int>();

        public Dictionary<string, int> commandsIssued = new Dictionary<string, int>();
        public Dictionary<string, int> donations = new Dictionary<string, int>();
        public string mostRecentDonation = "";

        public double lowestSuccessfulTinkerChance = 100;
        public string lowestSuccessfulTinkerChanceDescription = ""; // Steel(wk1) to the Iron Celdon Leggings(wk10, 9 tinks)
        public double highestFailedTinkerChance = 0;
        public string highestFailedTinkerChanceDescription = ""; // Steel(wk10) to the Iron Celdon Leggings(wk2, 0 tinks) for Sunnuj

        public int highestPlayerImbueLandedStreak = 0;
        public string highestPlayerImbueLandedStreakName = "";
        public int highestPlayerImbueFailedStreak = 0;
        public string highestPlayerImbueFailedStreakName = "";

        public int currentImbueLandedStreak = 0;
        public int highestImbueLandedStreak = 0;
        public int currentImbueFailedStreak = 0;
        public int highestImbueFailedStreak = 0;

        public Dictionary<string, int> portalsSummoned = new Dictionary<string, int>();

        public int selfBuffsCasted = 0;
        public int playerBuffsCasted = 0;
        public int buffProfilesCasted = 0;
        public int fizzles = 0;
        public Dictionary<string, int> burnedComponents = new Dictionary<string, int>();
        public ulong operatingCost = 0;
        public ulong operatingRevenue = 0;
        public ulong timeSpentBuffing = 0;
        public ulong timeSpentSelfBuffing = 0;
        public ulong uptime = 0;
        public ulong startingUptime = 0;

        public GlobalStats() {

        }

        public string GetFriendlyUptime() {
            return Util.GetFriendlyTimeDifference(uptime);
        }

        public ulong GetUptime() {
            return uptime;
        }

        internal List<string> GetImbueTypes() {
            var types = new List<string>();

            foreach (var k in imbuesLandedBySalvageType.Keys) {
                if (!types.Contains(k)) types.Add(k);
            }

            foreach (var k in imbuesFailedBySalvageType.Keys) {
                if (!types.Contains(k)) types.Add(k);
            }

            return types;
        }

        public void AddBurnedComponent(string component, int amount) {
            if (!burnedComponents.ContainsKey(component)) {
                burnedComponents.Add(component, 0);
            }

            burnedComponents[component] += amount;
        }

        public void AddSalvageBagApplied(string salvageType, int amount) {
            if (!salvageBagsUsed.ContainsKey(salvageType)) {
                salvageBagsUsed.Add(salvageType, 0);
            }

            salvageBagsUsed[salvageType] += amount;
        }

        public void AddItemBlownUpBySalvageType(string salvageType, int amount) {
            if (!nonImbuesFailed.ContainsKey(salvageType)) {
                nonImbuesFailed.Add(salvageType, 0);
            }

            nonImbuesFailed[salvageType] += amount;
        }

        public void AddImbueLandedBySalvageType(string salvageType, int amount) {
            if (!imbuesLandedBySalvageType.ContainsKey(salvageType)) {
                imbuesLandedBySalvageType.Add(salvageType, 0);
            }

            imbuesLandedBySalvageType[salvageType] += amount;
        }

        public void AddImbueFailedBySalvageType(string salvageType, int amount) {
            if (!imbuesFailedBySalvageType.ContainsKey(salvageType)) {
                imbuesFailedBySalvageType.Add(salvageType, 0);
            }

            imbuesFailedBySalvageType[salvageType] += amount;
        }

        private static string GetGlobalsStatsFilePath() {
            return Path.Combine(Util.GetCharacterDataDirectory(), "stats.json");
        }

        private void NormalizeCollections() {
            salvageBagsUsed = salvageBagsUsed ?? new Dictionary<string, int>();
            nonImbuesFailed = nonImbuesFailed ?? new Dictionary<string, int>();
            imbuesLandedBySalvageType = imbuesLandedBySalvageType ?? new Dictionary<string, int>();
            imbuesFailedBySalvageType = imbuesFailedBySalvageType ?? new Dictionary<string, int>();
            commandsIssued = commandsIssued ?? new Dictionary<string, int>();
            donations = donations ?? new Dictionary<string, int>();
            portalsSummoned = portalsSummoned ?? new Dictionary<string, int>();
            burnedComponents = burnedComponents ?? new Dictionary<string, int>();
        }

        private static GlobalStats TryLoadStatsFile(string path) {
            try {
                if (!File.Exists(path)) return null;

                var stats = JsonConvert.DeserializeObject<GlobalStats>(File.ReadAllText(path));
                if (stats == null) {
                    throw new InvalidDataException("The statistics file is empty or contains JSON null.");
                }

                stats.NormalizeCollections();
                return stats;
            }
            catch (Exception ex) {
                Util.LogException(new InvalidDataException("Unable to load statistics from " + path, ex));
                return null;
            }
        }

        internal static GlobalStats Load() {
            try {
                string path = GetGlobalsStatsFilePath();
                // An older interrupted save may have left a complete .new file.
                foreach (string candidate in new[] { path, path + ".new", path + ".bak" }) {
                    var stats = TryLoadStatsFile(candidate);
                    if (stats != null) {
                        if (candidate != path) {
                            Util.WriteToChat("DoThingsBot recovered statistics from " + candidate);
                        }
                        return stats;
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }

            // A bad optional statistics file must never prevent the bot from starting.
            return new GlobalStats();
        }

        internal void Save() {
            try {
                NormalizeCollections();
                string path = GetGlobalsStatsFilePath();
                string pendingPath = path + ".new";
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                using (var stream = new FileStream(pendingPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) {
                        writer.Write(json);
                        writer.Flush();
                        stream.Flush(true);
                    }
                }

                if (File.Exists(path)) {
                    if (TryLoadStatsFile(path) != null) {
                        File.Replace(pendingPath, path, path + ".bak");
                    }
                    else {
                        // Keep the failed file without replacing a known-good backup with it.
                        string preservedPath = path + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "." + Guid.NewGuid().ToString("N");
                        File.Copy(path, preservedPath, false);
                        File.Replace(pendingPath, path, null);
                    }
                }
                else {
                    File.Move(pendingPath, path);
                }

                if (Globals.Stats != null) Globals.Stats.lastGlobalStatsSave = DateTime.UtcNow;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        internal int GetTotalSalvageBagsUsed() {
            var total = 0;

            foreach (var key in salvageBagsUsed.Keys) {
                total += salvageBagsUsed[key];
            }

            return total;
        }

        internal int GetFailedImbueCount() {
            var total = 0;

            foreach (var key in imbuesFailedBySalvageType.Keys) {
                total += imbuesFailedBySalvageType[key];
            }

            return total;
        }

        internal int GetSucceededImbueCount() {
            var total = 0;

            foreach (var key in imbuesLandedBySalvageType.Keys) {
                total += imbuesLandedBySalvageType[key];
            }

            return total;
        }

        internal double GetOverallImbuePercentage() {
            var failed = GetFailedImbueCount();
            var succeeded = GetSucceededImbueCount();
            return Math.Round((double)succeeded / (double)(failed + succeeded), 4) * 100;
        }

        internal object GetTotalImbueAttempts() {
            var failed = GetFailedImbueCount();
            var succeeded = GetSucceededImbueCount();

            return failed + succeeded;
        }

        internal string GetImbueTypeStats(string type) {
            var failed = imbuesFailedBySalvageType.ContainsKey(type) ? imbuesFailedBySalvageType[type] : 0;
            var landed = imbuesLandedBySalvageType.ContainsKey(type) ? imbuesLandedBySalvageType[type] : 0;
            var total = landed + failed;
            var percent = (Math.Round((double)landed/total, 4) * 100) + "%";

            return string.Format("{0}", percent);
        }

        internal Dictionary<string, string> GetImbueTypeStatsList() {
            Dictionary<string, string> stats = new Dictionary<string, string>();

            foreach (var type in Globals.Stats.globalStats.GetImbueTypes()) {
                stats.Add(type, Globals.Stats.globalStats.GetImbueTypeStats(type));
            }

            return stats;
        }

        internal int GetTotalCommandsIssued() {
            int total = 0;

            foreach (var key in commandsIssued.Keys) {
                total += commandsIssued[key];
            }

            return total;
        }

        internal int GetTotalDonations() {
            int total = 0;

            foreach (var key in donations.Keys) {
                total += donations[key];
            }

            return total;
        }

        internal int GetTotalBurnedComponents() {
            int total = 0;

            foreach (var key in burnedComponents.Keys) {
                total += burnedComponents[key];
            }

            return total;
        }

        internal int GetTotalPortalsSummoned() {
            int total = 0;

            foreach (var key in portalsSummoned.Keys) {
                total += portalsSummoned[key];
            }

            return total;
        }

        internal int GetTotalPlayersServed() {
            return Directory.GetFiles(Util.GetPlayerDataDirectory(), "*.json", SearchOption.TopDirectoryOnly).Length;
        }
    }
}
