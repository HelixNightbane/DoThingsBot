using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.Lib {
    public static class DangerousMonsterDetector {
        static DateTime lastThink = DateTime.UtcNow;

        public static void Think() {
            if (DateTime.UtcNow - lastThink > TimeSpan.FromSeconds(5)) {
                lastThink = DateTime.UtcNow;
                bool hasDangerousMonsterNearby = false;
                string dangerousMonsterName = "Monster";
                double shortestDistance = 1000;
                
                foreach (var obj in CoreManager.Current.WorldFilter.GetByObjectClass(Decal.Adapter.Wrappers.ObjectClass.Monster)) {
                    // ignore player summoned pets
                    if (Util.IsCombatPet(obj)) {
                        continue;
                    }
                    
                    var distance = Util.GetDistanceFromPlayer(obj);
                    if (distance <= Config.Bot.DangerousMonsterLogoffDistance.Value && !Config.Bot.HarmlessMonsterWeenies.Value.Contains(obj.Type)) {
                        if (distance < shortestDistance) {
                            hasDangerousMonsterNearby = true;
                            dangerousMonsterName = obj.Name;
                            shortestDistance = distance;
                        }
                    }
                }

                if (hasDangerousMonsterNearby) {
                    Chat.ChatManager.Say($"I'm logging off because a dangerous {dangerousMonsterName} is {Math.Round(shortestDistance, 2)}m away! -b-");
                    CoreManager.Current.Actions.Logout();
                }
            }
        }
    }
}
