using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    public class BotIdleState : IBotState {
        public string Name { get => "BotIdleState"; }
        private DateTime firstThought = DateTime.UtcNow;
        private DateTime lastThought = DateTime.MinValue;
        private DateTime lastBuffCheck = DateTime.MinValue;
        private bool needsToEquip = false;

        public void Enter(Machine machine) {
            //Util.WriteToChat("Entered Idle State.");

            if (Config.Tinkering.KeepEquipmentOnDelay.Value > 0) {
                needsToEquip = true;
            }

            Globals.Core.Actions.FaceHeading(Config.Bot.DefaultHeading.Value, true);
        }

        public void Exit(Machine machine) {
            //Util.WriteToChat("Exited Idle State.");
        }

        public void Think(Machine machine) {
            if (Globals.DoThingsBot.needsEquipmentCheck && needsToEquip && DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(Config.Tinkering.KeepEquipmentOnDelay.Value)) {
                needsToEquip = false;

                ItemBundle itemBundle = new ItemBundle();
                itemBundle.SetCraftMode(CraftMode.None);
                itemBundle.SetEquipMode(EquipMode.Idle);

                Globals.DoThingsBot.needsEquipmentCheck = false;

                machine.ChangeState(new BotEquipItemsState(itemBundle));
                return;
            }

            if (DateTime.UtcNow - lastThought > TimeSpan.FromSeconds(5)) {
                lastThought = DateTime.UtcNow;

                var wanderDistance = BotStickyState.GetDistanceToStickySpot();
                if (Config.Bot.EnableStickySpot.Value && wanderDistance > Config.Bot.StickySpotMaxDistance.Value && wanderDistance < 100) {
                    Util.WriteToChat($"Bot has wandered {wanderDistance} (max {Config.Bot.StickySpotMaxDistance.Value}). Switching to BotStickyState");
                    machine.ChangeState(new BotStickyState());
                    return;
                }
                
                Globals.Core.Actions.FaceHeading(Config.Bot.DefaultHeading.Value, true);

                if (DateTime.UtcNow - machine.GetDateTimeValue("lastBuffCheck") > TimeSpan.FromSeconds(60)) {
                    machine.SetValue("lastBuffCheck", DateTime.UtcNow);

                    if (Spells.DoesAnySpellNeedRefresh(Config.Bot.GetWantedIdleEnchantments())) {
                        ItemBundle itemBundle = new ItemBundle();
                        itemBundle.SetCraftMode(CraftMode.None);
                        itemBundle.SetEquipMode(EquipMode.Buff);

                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                    }
                    return;
                }
            }

        }

        public ItemBundle GetItemBundle() {
                return null;
        }
    }
}
