using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.Stats {
    public static class StatAnnouncements {
        public static string GetRandom() {
            var stats = Globals.Stats.globalStats;

            var messages = new List<string>();

            if (stats.GetTotalPlayersServed() == 0) return null;

            var uptime = Util.GetFriendlyTimeDifference(stats.GetUptime());

            if (Globals.Stats.GetTotalBurnedComponents() > 5) {
                // TODO: I have burned {burned_comps} and will last for another {predicted_runtime} until I need more components.
                ulong lowest = ulong.MaxValue;
                var lowestKey = "None";

                foreach (var comp in Globals.Stats.burnedComponents) {
                    ulong avgBurnTime = Globals.Stats.GetUptime() / (ulong)comp.Value;
                    ulong predicted = avgBurnTime * (ulong)Util.GetItemCount(comp.Key);

                    if (predicted < lowest) {
                        lowest = predicted;
                        lowestKey = comp.Key;
                    }
                }

                if (lowestKey != "None") {
                    var duration = Util.GetFriendlyTimeDifference(Globals.Stats.GetUptime());
                    var predictedRuntime = Util.GetFriendlyTimeDifference(lowest);
                    var count = (ulong)Util.GetItemCount(lowestKey);

                    messages.Add(string.Format("I have burned {0} {1}s (out of {2}) in {3}. I will run out in {4}.", Globals.Stats.burnedComponents[lowestKey], lowestKey, count, duration, predictedRuntime));
                }
            }

            messages.Add(string.Format("I have served {0} total commands from {1} unique characters over {2}.", stats.GetTotalCommandsIssued(), stats.GetTotalPlayersServed(), uptime));

            if (stats.playerBuffsCasted > 0) {
                messages.Add(string.Format("I have spent {0} casting {1} buffs on others.  I fizzled {2} times and burned {3} components.", Util.GetFriendlyTimeDifference(stats.timeSpentBuffing), stats.playerBuffsCasted, stats.fizzles, stats.GetTotalBurnedComponents()));
            }

            if (stats.GetTotalPortalsSummoned() > 0) {
                messages.Add(string.Format("I have summoned a total of {0} portals over {1}.", stats.GetTotalPortalsSummoned(), uptime));
            }

            if (stats.GetTotalSalvageBagsUsed() > 0) {
                messages.Add(string.Format("I have used {0} total bags of salvage, {1} of those were imbues. I landed imbues {2}% overall.", stats.GetTotalSalvageBagsUsed(), stats.GetTotalImbueAttempts(), stats.GetOverallImbuePercentage()));

                if (stats.highestFailedTinkerChance > 0) {
                    messages.Add(string.Format("My highest failed tinker chance was {0} when I applied {1}", stats.highestFailedTinkerChance, stats.highestFailedTinkerChanceDescription));
                }
                if (stats.lowestSuccessfulTinkerChance < 100) {
                    messages.Add(string.Format("My lowest successful tinker chance was {0} when I applied {1}", stats.lowestSuccessfulTinkerChance, stats.lowestSuccessfulTinkerChanceDescription));
                }
            }

            if (messages.Count == 0) return null;

            var r = new Random();

            return messages[r.Next(0, messages.Count)];
        }
    }
}
