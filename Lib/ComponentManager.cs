using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib {
    class Component {
        public string Name { get; set; }
        public int Icon { get; set; }
        public string ConfigKey { get; set; }

        public Component(string name, int icon, string configKey = "") {
            Name = name;
            Icon = icon;
            ConfigKey = string.IsNullOrEmpty(configKey) ? GenerateConfigKey() : configKey;
        }

        private string GenerateConfigKey() {
            return string.Format("{0}LowCount", Name.Replace(" ", ""));
        }

        public int LowWarningAmount() {
            return Config.Bot.GetComponentLowWarningLevel(ConfigKey);
        }

        public int Count() {
            return Util.GetItemCount(Name);
        }

        public bool IsLow() {
            return LowWarningAmount() != 0 && Count() <= LowWarningAmount();
        }
    }

    public static class ComponentManager {
        internal static List<Component> trackedComponents = new List<Component>();

        static ComponentManager() {
            trackedComponents.Add(new Component("Prismatic Taper", 9770));
            trackedComponents.Add(new Component("Mana Scarab", 26533));
            trackedComponents.Add(new Component("Platinum Scarab", 8033));
            trackedComponents.Add(new Component("Pyreal Scarab", 5096));
            trackedComponents.Add(new Component("Gold Scarab", 5093));
            trackedComponents.Add(new Component("Silver Scarab", 5097));
            trackedComponents.Add(new Component("Copper Scarab", 5092));
            trackedComponents.Add(new Component("Iron Scarab", 5094));
            trackedComponents.Add(new Component("Lead Scarab", 5095));
        }

        internal static bool IsLowOnComps() {
            foreach (var comp in trackedComponents) {
                if (comp.IsLow()) {
                    return true;
                }
            }

            var gems = Config.Portals.GetUniqueGemNames();
            foreach (var gem in gems) {
                var min = Config.Portals.PortalGemLowCount.Value;
                if (min >= 0 && Util.GetItemCount(gem) <= min) {
                    return true;
                }
            }

            return false;
        }

        internal static string LowComponentAnnouncement() {
            string message = "";
            var lowComponents = new List<string>();
            var emptyComponents = new List<string>();

            foreach (var comp in trackedComponents) {
                if (comp.LowWarningAmount() == 0) continue;
                if (comp.Count() == 0) {
                    emptyComponents.Add(comp.Name + "s");
                }
                else if (comp.IsLow()) {
                    lowComponents.Add(comp.Name + "s");
                }
            }

            var gems = Config.Portals.GetUniqueGemNames();
            foreach (var gem in gems) {
                var min = Config.Portals.PortalGemLowCount.Value;

                if (min <= 0) continue;
                
                var count = Util.GetItemCount(gem);

                if (count == 0) {
                    emptyComponents.Add(gem + "s");
                }
                else if (Util.GetItemCount(gem) <= min) {
                    lowComponents.Add(gem + "s");
                }
            }

            var lowCompString = string.Join(", ", lowComponents.ToArray());
            var emptyCompString = string.Join(", ", emptyComponents.ToArray());

            if (emptyComponents.Count > 0 && lowComponents.Count > 0) {
                message = string.Format("I am out of: {0}. I am running low on: {1}", emptyCompString, lowCompString);
            }
            else if (emptyComponents.Count > 0) {
                message = string.Format("I am out of: {0}", emptyCompString);
            }
            else if (lowComponents.Count > 0) {
                message = string.Format("I am running low on: {0}", lowCompString);
            }


            return message;
        }
    }
}
