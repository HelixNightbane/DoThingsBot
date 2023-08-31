using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot {
    public class PlayerData {
        public string PlayerName = "unknown";

        public long balance = 0;

        public double lowestSuccessfulTinkerChance = 100;
        public string lowestSuccessfulTinkerChanceDescription = ""; // Steel(wk1) on Iron Celdon Leggings)
        public double highestFailedTinkerChance = 0;
        public string highestFailedTinkerChanceDescription = ""; // Steel(wk10) on Iron Celdon Leggings

        public int currentImbueLandedStreak = 0;
        public int highestImbueLandedStreak = 0;
        public int currentImbueFailedStreak = 0;
        public int highestImbueFailedStreak = 0;

        public Dictionary<string, int> salvageBagsUsed = new Dictionary<string, int>();
        public Dictionary<string, int> itemsBlownUpBySalvageType = new Dictionary<string, int>();
        public Dictionary<string, int> imbuesLandedBySalvageType = new Dictionary<string, int>();
        public Dictionary<string, int> imbuesFailedBySalvageType = new Dictionary<string, int>();

        public Dictionary<string, int> portalsSummoned = new Dictionary<string, int>();

        public Dictionary<string, int> commandsIssued = new Dictionary<string, int>();

        public int totalBuffsCast = 0;
        public int totalBuffProfilesCast = 0;
        public int fizzles = 0;
        public ulong totalTimeSpentBuffing = 0;

        public List<int> itemIds = new List<int>();
        public List<int> buffIds = new List<int>();
        public List<int> stolenItemIds = new List<int>();
        public List<int> missingItemIds = new List<int>();
        public Dictionary<int, string> itemDescriptions = new Dictionary<int, string>();
        public Dictionary<int, string> itemNames = new Dictionary<int, string>();
        public Dictionary<string, int> burnedComponents = new Dictionary<string, int>();
        public Dictionary<string, int> donations = new Dictionary<string, int>();

        public string recipe = "";
        public string jobType = "";

        public PlayerData(string owner) {
            PlayerName = owner;
        }

        public void AddBurnedComponent(string component, int count) {
            if (burnedComponents.ContainsKey(component)) {
                burnedComponents[component] += count;
            }
            else {
                burnedComponents.Add(component, count);
            }
        }

        public void AddDonation(string item, int count) {
            if (donations.ContainsKey(item)) {
                donations[item] += count;
            }
            else {
                donations.Add(item, count);
            }
        }

        public int GetTotalCommandsIssued() {
            var total = 0;

            foreach (var command in commandsIssued.Keys) {
                total += commandsIssued[command];
            }

            return total;
        }

        internal string GetImbueTypeStats(string type) {
            var failed = imbuesFailedBySalvageType.ContainsKey(type) ? imbuesFailedBySalvageType[type] : 0;
            var landed = imbuesLandedBySalvageType.ContainsKey(type) ? imbuesLandedBySalvageType[type] : 0;
            var total = landed + failed;
            var percent = (Math.Round((double)landed / total, 4) * 100) + "%";

            return string.Format("{0}", percent);
        }

        internal Dictionary<string, string> GetImbueTypeStatsList() {
            Dictionary<string, string> stats = new Dictionary<string, string>();

            foreach (var type in GetImbueTypes()) {
                stats.Add(type, GetImbueTypeStats(type));
            }

            return stats;
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
    }
}
