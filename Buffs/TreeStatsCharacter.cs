using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.Buffs {
    public class TreeStatsCharacter {
        public string PlayerName = "unknown";
        public string updated_at = "unkown";
        public Dictionary<string, Dictionary<string,string>> skills = new Dictionary<string, Dictionary<string, string>>();

        public TreeStatsCharacter(string owner) {
            PlayerName = owner;
        }

        public List<string> GetTrainedSkills() {
            var trainedSkills = new List<string>();
            try {
                foreach (var skey in skills.Keys) {
                    if (skills[skey]["training"] != "Untrained" && skills[skey]["training"] != "Unusable") {
                        trainedSkills.Add(skills[skey]["name"]);
                    }
                }
            }
            catch (Exception ex) {
            }

            return trainedSkills;
        }
    }
}
